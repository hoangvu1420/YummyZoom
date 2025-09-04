## Gaps / TODOs

Customizations:
Missing commands/endpoints: assign/remove customization group on item (domain supports it).
Missing projector handlers for CustomizationGroup/Choice/Tag aggregate events (to rebuild affected menus).
Management queries: not implemented (e.g., GetMenusForManagement, GetMenuCategoryDetails, GetMenuItemsByCategory, GetMenuItemDetails).
Menu removal: no handler for MenuRemoved; current disabled handler deletes the view, but removal should also delete.
Backfill/reconciliation:
No one-time job to build all FullMenuView rows; no periodic recon job.
Admin trigger:
Manual rebuild command exists but no API endpoint to invoke it (if desired).
Search:
Geo filtering not yet implemented (currently text/cuisine stub as intended).


## Quick Next Steps

Implement missing commands + endpoints:
  Assign/Remove customization group to menu item.
  Optional: dedicated UpdatePrice if you want separate semantics.
Add projector handlers for:
  CustomizationGroupCreated/Deleted/ChoiceAdded/Removed/Updated, Tag changes.
Implement management read-side queries (DTOs + Dapper) for staff UI.
Add MenuRemovedEventHandler to delete FullMenuView.
Add one-shot backfill and periodic reconciliation hosted service using IMenuReadModelRebuilder.
Optionally expose an admin endpoint for RebuildFullMenu (protected by admin policy).
Extend SearchRestaurantsQueryHandler with geospatial filters (PostGIS ST_DWithin) when ready.