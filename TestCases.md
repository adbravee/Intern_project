# Test Cases (ItemProcessingApp)

## A) Login
1. `TC-LOGIN-001` Valid login
   - Steps: Enter `admin` / `Admin@123` and submit.
   - Expected: Redirect to `/Items/Index` and username appears in navbar.
2. `TC-LOGIN-002` Invalid username
   - Steps: Enter wrong username, correct password.
   - Expected: Validation message “Invalid username or password.”
3. `TC-LOGIN-003` Invalid password
   - Steps: Enter correct username, wrong password.
   - Expected: Validation message “Invalid username or password.”
4. `TC-LOGIN-004` Empty username/password
   - Steps: Submit with empty fields.
   - Expected: Field validation messages for required `Username` and `Password`.
5. `TC-LOGIN-005` Remember me
   - Steps: Login with “Remember me” checked.
   - Expected: Session persists according to configured cookie expiry.

## B) Item Screen (CRUD + Search)
1. `TC-ITEM-001` Create root item (no parent)
   - Steps: Go to `/Items/Create`, set `Name` + `Weight`, leave `ParentId` blank.
   - Expected: Item is created and appears in `/Items` list.
2. `TC-ITEM-002` Create child item (using ParentId dropdown)
   - Steps: Create item with `ParentId` pointing to an existing item.
   - Expected: Item appears under parent in `Details` and `Tree`.
3. `TC-ITEM-003` Create item with empty name
   - Steps: Submit Create with `Name=""`.
   - Expected: “Name is required.” shown and item not created.
4. `TC-ITEM-004` Create item with invalid weight (0 / negative)
   - Steps: Submit with `Weight=0` or `Weight=-1`.
   - Expected: “Weight must be greater than 0.” shown.
5. `TC-ITEM-005` Edit item values
   - Steps: Edit existing item name/weight/parent.
   - Expected: Updated values appear in list and details.
6. `TC-ITEM-006` Edit circular parent assignment
   - Steps: Attempt to set an item’s parent to its own ancestor/itself.
   - Expected: Error “Cannot set this parent: it would create a circular reference.” and save blocked.
7. `TC-ITEM-007` Delete leaf item
   - Steps: Delete an item with no children.
   - Expected: Item removed; success message shown.
8. `TC-ITEM-008` Delete parent item with children
   - Steps: Delete an item that has children.
   - Expected: Deletion blocked with TempData error telling to remove/re-parent children first.
9. `TC-ITEM-009` Search by name
   - Steps: Create multiple items, search by partial name from `/Items`.
   - Expected: Filtered list only shows matching items.
10. `TC-ITEM-010` Clear search
   - Steps: Click “Clear”.
   - Expected: Returns to full list.

## C) Process Item (Parent + Multiple Output Children)
1. `TC-PROC-001` Process item with single output
   - Steps: Go to `/Items/Process`, select a parent, enter 1 output (name + valid weight), submit.
   - Expected: New child item created under selected parent; redirect to parent `Details`.
2. `TC-PROC-002` Process item with multiple outputs
   - Steps: Add 3+ output rows, fill all rows, submit.
   - Expected: All outputs created and appear in parent `Details` and `Tree`.
3. `TC-PROC-003` Missing parent
   - Steps: Submit with no parent selected.
   - Expected: Validation “Parent item is required.”
4. `TC-PROC-004` Output row missing name
   - Steps: Leave an output name empty.
   - Expected: “Output name is required.” shown; submit blocked.
5. `TC-PROC-005` Output row invalid weight
   - Steps: Enter `Weight=0` for an output.
   - Expected: “Weight must be greater than 0.” shown.
6. `TC-PROC-006` Long output name (length > 100)
   - Steps: Enter a name longer than 100 chars.
   - Expected: “Output name cannot exceed 100 characters.” shown.
7. `TC-PROC-007` Process non-existent parent (tampered request)
   - Steps: POST with an invalid `ParentId`.
   - Expected: Error “Selected parent item does not exist.” and nothing created.

## D) List of Processed Items
1. `TC-PROCESSED-001` Processed list shows items with outputs
   - Steps: Process a parent, then open `/Items/Processed`.
   - Expected: Parent appears in processed list with outputs count > 0.
2. `TC-PROCESSED-002` Processed list search
   - Steps: Search by part of processed item’s name.
   - Expected: Filtered results only.
3. `TC-PROCESSED-003` No processed items state
   - Steps: Fresh DB with no children.
   - Expected: “No processed items found.” message.

## E) Tree View
1. `TC-TREE-001` Expand/collapse works
   - Steps: Open `/Items/Tree`, click node toggles + Expand All.
   - Expected: UI shows/hides descendants correctly.
2. `TC-TREE-002` Correct hierarchy depth and counts
   - Steps: Create multi-level tree, view badges and child count.
   - Expected: Depth badge (`Level`) and child counts are accurate.

## Optional: Automation (Python + Selenium)
Recommended automated flows:
- Login (valid and invalid).
- Create root item.
- Process item with 2 output rows.
- Verify processed items list contains the processed parent.

