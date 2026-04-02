# UI/UX Screens (ItemProcessingApp)

## 1) Login
- Route: `/Account/Login` (GET/POST)
- Elements: `Username`, `Password`, `Remember me`, validation summary + field messages.
- Design decision: clear labels + Bootstrap input groups for fast recognition.

## 2) Item Screen (CRUD + Search)
- List/Search: `/Items` (GET)
  - Search bar filters by `Name` using `Index(string? search)`.
  - Each row provides Actions: `Details`, `Edit`, `Add Child`, `Delete`.
- Create: `/Items/Create` (GET/POST)
  - Fields: `Name` (required), `Weight` (number > 0), optional `ParentId` dropdown (root supported).
  - Validation messages shown next to inputs.
- Edit: `/Items/Edit/{id}` (GET/POST)
  - Same validation rules as Create.
  - Circular-parent assignments blocked server-side.
- Delete: `/Items/Delete/{id}` (GET/POST)
  - Confirmation page blocks deletion when the item has children.

## 3) Process Item (Parent + Multiple Outputs)
- Route: `/Items/Process` (GET/POST)
- Flow:
  - Select `Parent Item` (dropdown).
  - Add one or more `Output Items` rows (name + weight).
  - Submit creates new child items with `ParentId = selected parent`.
  - After processing, user is redirected to the parent `Details`.
- Design decision: dynamic rows allow “one-to-many” outputs without multiple page visits; validation highlights missing name/weight.

## 4) List of Processed Items
- Route: `/Items/Processed` (GET)
- Definition used in UI: “Processed” means “items that already have outputs (children)”.
- Displays: `Id`, `Name`, `Weight`, `Outputs count`, actions to `Details` and `Tree`.
- Includes search by `Name`.

## 5) Parent/Child Item Tree Structure
- Route: `/Items/Tree` (GET)
- Uses an expandable/collapsible hierarchy view:
  - `_TreeNode` partial renders each node recursively.
  - Expand/Collapse per node + Expand All / Collapse All.
  - Badges show depth (`Level`) and child count.

## Suggested User Flow
1. Login.
2. Create root item(s) and optional intermediate items.
3. Use **Process Item** to attach multiple output children (with their weights).
4. View **Processed Items** list to see what has been processed.
5. Use **Tree View** to verify the parent/child hierarchy.

