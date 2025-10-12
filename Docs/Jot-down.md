Here are PostgreSQL queries you can run to verify the bundle seeding end-to-end, plus what you should expect to see given your run.

- Sanity counts
  - Verifies total rows across key tables.
    ```sql
    SELECT
      (SELECT COUNT(*) FROM "Restaurants")            AS restaurants,
      (SELECT COUNT(*) FROM "Menus")                  AS menus,
      (SELECT COUNT(*) FROM "MenuCategories")         AS menu_categories,
      (SELECT COUNT(*) FROM "MenuItems")              AS menu_items,
      (SELECT COUNT(*) FROM "CustomizationGroups")    AS customization_groups,
      (SELECT COUNT(*) FROM "CustomizationChoices")   AS customization_choices,
      (SELECT COUNT(*) FROM "Tags")                   AS tags;
    ```
  - Expected: restaurants=3, menus=3, menu_categories=12, menu_items=72, customization_groups=6, customization_choices=21, tags=12.

- Restaurant details
  - Spot check core fields and owned VOs mapping.
    ```sql
    SELECT
      r."Name", r."CuisineType", r."IsVerified", r."IsAcceptingOrders",
      r."Location_City" AS city, r."ContactInfo_Email" AS email, r."BusinessHours"
    FROM "Restaurants" r
    ORDER BY r."Name";
    ```
  - Expected rows (3):
    - Bella Vista Italian | Italian | t | t | Downtown | orders@bellavista.com | 11:00-22:00
    - Sakura Sushi | Japanese | t | t | Midtown | hello@sakurasushi.com | 10:30-21:30
    - El Camino Taqueria | Mexican | t | t | Uptown | contact@elcamino.com | 11:00-23:00

- Per-restaurant content summary
  - Ensures graph completeness per restaurant.
    ```sql
    SELECT
      r."Name",
      COUNT(DISTINCT m."Id") AS menus,
      COUNT(DISTINCT c."Id") AS categories,
      COUNT(DISTINCT i."Id") AS items,
      COUNT(DISTINCT g."Id") AS groups
    FROM "Restaurants" r
    LEFT JOIN "Menus" m ON m."RestaurantId" = r."Id"
    LEFT JOIN "MenuCategories" c ON c."MenuId" = m."Id"
    LEFT JOIN "MenuItems" i ON i."RestaurantId" = r."Id"
    LEFT JOIN "CustomizationGroups" g ON g."RestaurantId" = r."Id"
    GROUP BY r."Name"
    ORDER BY r."Name";
    ```
  - Expected per restaurant: menus=1, categories=4, items=24, groups=2.

- Categories and display order
  - Verifies menu structure and ordering.
    ```sql
    SELECT
      r."Name" AS restaurant, c."Name" AS category, c."DisplayOrder"
    FROM "Restaurants" r
    JOIN "Menus" m ON m."RestaurantId" = r."Id"
    JOIN "MenuCategories" c ON c."MenuId" = m."Id"
    ORDER BY r."Name", c."DisplayOrder";
    ```
  - Expected per restaurant: Appetizers(1), Mains(2), Desserts(3), Drinks(4).

- Items with assigned customizations
  - Confirms items reference customization groups (JSONB).
    ```sql
    SELECT
      r."Name", COUNT(*) AS items_with_customizations
    FROM "Restaurants" r
    JOIN "MenuItems" i ON i."RestaurantId" = r."Id"
    WHERE jsonb_array_length(i."AppliedCustomizations"::jsonb) > 0
    GROUP BY r."Name"
    ORDER BY r."Name";
    ```
  - Expected: 3 per restaurant (Margherita Pizza, BBQ Chicken Pizza, Classic Cheeseburger) → 3 | 3 | 3.

- Customizations on a known item
  - Shows human titles and order assigned to an item.
    ```sql
    SELECT
      r."Name" AS restaurant, c."Name" AS category, i."Name" AS item,
      ac->>'DisplayTitle' AS group_title, (ac->>'DisplayOrder')::int AS display_order
    FROM "MenuItems" i
    JOIN "MenuCategories" c ON c."Id" = i."MenuCategoryId"
    JOIN "Menus" m ON m."Id" = c."MenuId"
    JOIN "Restaurants" r ON r."Id" = m."RestaurantId",
    LATERAL jsonb_array_elements(i."AppliedCustomizations"::jsonb) AS ac
    WHERE i."Name" = 'Margherita Pizza'
    ORDER BY r."Name", display_order;
    ```
  - Expected per restaurant: Size (1), Extras (2).

- Items with dietary tags
  - Confirms JSONB list presence.
    ```sql
    SELECT
      r."Name", COUNT(*) AS items_with_dietary_tags
    FROM "Restaurants" r
    JOIN "MenuItems" i ON i."RestaurantId" = r."Id"
    WHERE jsonb_array_length(COALESCE(i."DietaryTagIds"::jsonb, '[]'::jsonb)) > 0
    GROUP BY r."Name"
    ORDER BY r."Name";
    ```
  - Expected: 4 per restaurant (e.g., Bruschetta, Caprese Skewers, Veggie Stir Fry, Gelato Trio) → 4 | 4 | 4.

- Expand dietary tags to names
  - Cross-check tag IDs in JSONB against Tags table.
    ```sql
    WITH tag_ids AS (
      SELECT i."Id" AS menu_item_id, (jt->>'Value')::uuid AS tag_id
      FROM "MenuItems" i,
           LATERAL jsonb_array_elements(COALESCE(i."DietaryTagIds"::jsonb, '[]'::jsonb)) jt
    )
    SELECT
      r."Name" AS restaurant, i."Name" AS item, t."TagName", t."TagCategory"
    FROM tag_ids ti
    JOIN "MenuItems" i ON i."Id" = ti.menu_item_id
    JOIN "Tags" t ON t."Id" = ti.tag_id
    JOIN "MenuCategories" c ON c."Id" = i."MenuCategoryId"
    JOIN "Menus" m ON m."Id" = c."MenuId"
    JOIN "Restaurants" r ON r."Id" = m."RestaurantId"
    ORDER BY restaurant, item, t."TagName";
    ```
  - Expected samples: Bruschetta → Vegetarian; Veggie Stir Fry → Vegan, Gluten-Free; Gelato Trio → Vegetarian.

- Customization groups and choices per restaurant
  - Validates group modules and choice counts.
    ```sql
    SELECT
      r."Name" AS restaurant, g."GroupName", COUNT(ch."ChoiceId") AS choices
    FROM "CustomizationGroups" g
    JOIN "Restaurants" r ON r."Id" = g."RestaurantId"
    LEFT JOIN "CustomizationChoices" ch ON ch."CustomizationGroupId" = g."Id"
    GROUP BY r."Name", g."GroupName"
    ORDER BY r."Name", g."GroupName";
    ```
  - Expected per restaurant: Size → 3, Extras → 4.

- Price sanity for Mains
  - Confirms Money VO persisted as amount/currency.
    ```sql
    SELECT
      r."Name" AS restaurant, i."Name" AS item,
      i."BasePrice_Amount" AS price, i."BasePrice_Currency" AS currency
    FROM "MenuItems" i
    JOIN "MenuCategories" c ON c."Id" = i."MenuCategoryId"
    JOIN "Menus" m ON m."Id" = c."MenuId"
    JOIN "Restaurants" r ON r."Id" = m."RestaurantId"
    WHERE c."Name" = 'Mains'
    ORDER BY restaurant, item;
    ```
  - Expected samples (USD): Margherita Pizza 12.99; BBQ Chicken Pizza 13.99; Classic Cheeseburger 11.99; etc.

- Menu names per restaurant
  - Ensures 1 “Main Menu” per restaurant.
    ```sql
    SELECT r."Name", m."Name"
    FROM "Restaurants" r
    JOIN "Menus" m ON m."RestaurantId" = r."Id"
    ORDER BY 1;
    ```
  - Expected: all rows show “Main Menu”.
