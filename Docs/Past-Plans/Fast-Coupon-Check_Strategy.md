# The read models to keep hot

* **ActiveCouponsByRestaurant**(RestaurantID, CouponID, Code, Type, Value, Scope, MinOrderAmount, ValidityStart/End, IsEnabled)
  Filtered to only “can possibly apply *now*”.
* **ItemCouponMatrix**(CouponID, RestaurantID, MenuItemID, MenuCategoryID nullable)
  A pre-fanned-out mapping of which **items/categories** a coupon touches (scope = WholeOrder → special marker).
* **UserCouponEligibility**(UserID, CouponID, RemainingUses, IsEligible)
  Merges global usage + per-user usage and status.

(All three are append-only/event-sourced projections; nothing transactional here.)

# Fast evaluation pipeline (runs per cart refresh)

1. **Fetch candidates**
   `SELECT * FROM ActiveCouponsByRestaurant WHERE RestaurantID = ? AND now() BETWEEN ValidityStart AND ValidityEnd AND IsEnabled = true`
   and inner-join `UserCouponEligibility` for `(UserID, CouponID)` where `IsEligible = true` and `RemainingUses > 0`.

2. **Materialize “cart facts” once** (from your cart’s snapshots)

   * For each line: `MenuItemID`, `MenuCategoryID`, `Quantity`, `UnitPriceAtOrder` (base + customizations), `LineTotal = UnitPriceAtOrder * Quantity`.
   * Aggregate once:

     * `cart_subtotal = SUM(LineTotal)`
     * `per_item_subtotal[MenuItemID]`
     * `per_category_subtotal[MenuCategoryID]`

3. **Compute eligible subtotal per coupon** (no scanning coupon rules):

   * If `Scope = WholeOrder`: `eligible = cart_subtotal`
   * If `Scope = SpecificItems`: `eligible = SUM(per_item_subtotal for items in ItemCouponMatrix where CouponID=…)`
   * If `Scope = SpecificCategories`: `eligible = SUM(per_category_subtotal for categories in ItemCouponMatrix where CouponID=…)`

4. **Apply the simple formula per coupon**
   (use cart snapshots so you always match the order math)

   * `Percentage`: `savings = round(eligible * (Value / 100))`
   * `FixedAmount`: `savings = LEAST(Value, eligible)`
   * `FreeItem` (Value = MenuItemID): if cart contains it, `savings = UnitPriceAtOrder(item)` (or that × free-qty if you allow >1); else `savings = 0`
   * Enforce `MinOrderAmount`: if `cart_subtotal < MinOrderAmount`, mark as **ineligible** and compute **gap = MinOrderAmount - cart\_subtotal** for a helpful hint.
   * Cap at zero and smallest currency unit; never exceed `eligible`.

5. **Rank & annotate for the UI**

   * Keep `Applicable = (savings > 0 and meets all constraints)`
   * **Sort** by `savings DESC`, then `ExpiresSooner`, then “easier to use” (WholeOrder > Category > Item).
   * Return a compact list: `[{code, label, savings, reasonIfIneligible, minOrderGap}]`

# One-query pattern (PostgreSQL example)

This does the math in a single round-trip; the cart arrives as JSON.

```sql
WITH cart AS (
  SELECT *
  FROM jsonb_to_recordset($1::jsonb)
       AS x(MenuItemID uuid, MenuCategoryID uuid, Qty int, UnitPrice numeric)
), cart_lines AS (
  SELECT MenuItemID, MenuCategoryID, Qty,
         (UnitPrice * Qty) AS LineTotal
  FROM cart
), cart_totals AS (
  SELECT
    SUM(LineTotal) AS cart_subtotal
  FROM cart_lines
), per_item AS (
  SELECT MenuItemID, SUM(LineTotal) AS item_subtotal
  FROM cart_lines GROUP BY MenuItemID
), per_cat AS (
  SELECT MenuCategoryID, SUM(LineTotal) AS cat_subtotal
  FROM cart_lines GROUP BY MenuCategoryID
), candidates AS (
  SELECT c.*
  FROM ActiveCouponsByRestaurant c
  JOIN UserCouponEligibility u USING (CouponID)
  WHERE c.RestaurantID = $2
    AND u.UserID = $3
    AND u.IsEligible = true
    AND u.RemainingUses > 0
    AND now() BETWEEN c.ValidityStart AND c.ValidityEnd
    AND c.IsEnabled = true
), elig AS (
  -- Whole order
  SELECT cand.CouponID, t.cart_subtotal AS eligible_subtotal
  FROM candidates cand CROSS JOIN cart_totals t
  WHERE cand.Scope = 'WholeOrder'
  UNION ALL
  -- Items
  SELECT cand.CouponID, COALESCE(SUM(pi.item_subtotal), 0) AS eligible_subtotal
  FROM candidates cand
  JOIN ItemCouponMatrix m ON m.CouponID = cand.CouponID AND m.MenuItemID IS NOT NULL
  LEFT JOIN per_item pi ON pi.MenuItemID = m.MenuItemID
  WHERE cand.Scope = 'SpecificItems'
  GROUP BY cand.CouponID
  UNION ALL
  -- Categories
  SELECT cand.CouponID, COALESCE(SUM(pc.cat_subtotal), 0) AS eligible_subtotal
  FROM candidates cand
  JOIN ItemCouponMatrix m ON m.CouponID = cand.CouponID AND m.MenuCategoryID IS NOT NULL
  LEFT JOIN per_cat pc ON pc.MenuCategoryID = m.MenuCategoryID
  WHERE cand.Scope = 'SpecificCategories'
  GROUP BY cand.CouponID
), quotes AS (
  SELECT cand.CouponID, cand.Code, cand.Type, cand.Value, cand.MinOrderAmount,
         e.eligible_subtotal,
         (SELECT cart_subtotal FROM cart_totals) AS cart_subtotal,
         CASE cand.Type
           WHEN 'Percentage'  THEN ROUND(e.eligible_subtotal * (cand.Value / 100.0), 2)
           WHEN 'FixedAmount' THEN LEAST(cand.Value, e.eligible_subtotal)
           WHEN 'FreeItem'    THEN (
             SELECT ROUND(MIN(UnitPrice), 2)  -- assumes 1 free; use SUM for N
             FROM cart c
             WHERE c.MenuItemID = cand.Value  -- Value holds the free MenuItemID
           )
         END AS raw_savings
  FROM candidates cand
  JOIN elig e USING (CouponID)
)
SELECT Code,
       GREATEST(COALESCE(raw_savings, 0), 0) AS savings,
       (cart_subtotal >= MinOrderAmount) AS meets_min_order,
       GREATEST(MinOrderAmount - cart_subtotal, 0) AS min_order_gap
FROM quotes
ORDER BY savings DESC NULLS LAST, MinOrderAmount ASC;
```

> Input `$1` is your cart lines JSON array; `$2 = RestaurantID`, `$3 = UserID`.

# Why this is fast

* The **heavy logic is precompiled** into the read models:

  * Which coupons exist and are active (no rule parsing on request).
  * What each coupon touches (ready-to-join matrix).
  * Whether the user may use each coupon (no counting on request).
* At request time you only:

  * Build tiny in-memory tables (`cart_lines`, `per_item`, `per_cat`).
  * Join once to compute `eligible_subtotal` per coupon.
  * Apply a **simple formula** per row.
    Complexity is roughly **O(#coupons + #cart\_lines)**; all operations are set-based and index-friendly.

# Indexing & storage tips

* Partition `ActiveCouponsByRestaurant` and `ItemCouponMatrix` by `RestaurantID`.
* Covering indexes:

  * `ItemCouponMatrix(CouponID, MenuItemID)` and `(CouponID, MenuCategoryID)`
  * `ActiveCouponsByRestaurant(RestaurantID, IsEnabled, ValidityStart, ValidityEnd)`
  * `UserCouponEligibility(UserID, CouponID, IsEligible)`
* Keep coupon rows **compact** (no big text blobs in the hot path).

# Nice UX extras you get “for free”

* Show **ineligible coupons** with a friendly reason: “Spend \$7.50 more to use SAVE10”.
* Badge the **best deal** (first row after sorting).
* If you allow only one coupon, you’re done; if you allow stacks, run a tiny knapsack on the already-computed `savings` with incompatibility rules—still fast because the candidate set is small.

If you want, I can adapt this to your exact schema names and produce a ready-to-run SQL function plus a TypeScript service wrapper.
