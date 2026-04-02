using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ItemProcessingApp.Data;
using ItemProcessingApp.Models;

namespace ItemProcessingApp.Controllers
{
    /// <summary>
    /// Handles all CRUD operations for Items, plus tree view, search, and AddChild.
    /// All actions require the user to be logged in ([Authorize]).
    /// </summary>
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(AppDbContext context, ILogger<ItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        // INDEX  –  list all items with optional search
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// GET /Items  or  GET /Items?search=foo
        /// Lists all items, filtered by name when a search term is supplied.
        /// </summary>
        public async Task<IActionResult> Index(string? search)
        {
            ViewData["CurrentSearch"] = search;

            // Start with all items, eagerly loading each item's Parent
            IQueryable<Item> query = _context.Items
                                             .Include(i => i.Parent)
                                             .OrderBy(i => i.Name);

            // Apply search filter (case-insensitive on SQL Server)
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.Name.Contains(search));

            var items = await query.ToListAsync();
            return View(items);
        }

        // ════════════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════════════

        /// <summary>GET /Items/Create</summary>
        public async Task<IActionResult> Create()
        {
            // Populate the optional "Parent" dropdown
            await PopulateParentDropDownAsync();
            return View();
        }

        /// <summary>POST /Items/Create</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Weight,ParentId")] Item item)
        {
            // Guard against circular references (a new item cannot be its own parent)
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(item);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Item \"{item.Name}\" created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating item.");
                    ModelState.AddModelError("", "An error occurred while saving. Please try again.");
                }
            }

            await PopulateParentDropDownAsync(item.ParentId);
            return View(item);
        }

        // ════════════════════════════════════════════════════════════════
        // EDIT
        // ════════════════════════════════════════════════════════════════

        /// <summary>GET /Items/Edit/5</summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var item = await _context.Items.FindAsync(id);
            if (item is null) return NotFound();

            await PopulateParentDropDownAsync(item.ParentId, excludeId: item.Id);
            return View(item);
        }

        /// <summary>POST /Items/Edit/5</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Weight,ParentId")] Item item)
        {
            if (id != item.Id) return NotFound();

            if (ModelState.IsValid)
            {
                // Prevent circular reference: item cannot be its own ancestor
                if (item.ParentId.HasValue && await IsCircularAsync(item.Id, item.ParentId.Value))
                {
                    ModelState.AddModelError("ParentId",
                        "Cannot set this parent: it would create a circular reference.");
                    await PopulateParentDropDownAsync(item.ParentId, excludeId: item.Id);
                    return View(item);
                }

                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Item \"{item.Name}\" updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.Id)) return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating item {Id}.", item.Id);
                    ModelState.AddModelError("", "An error occurred while saving. Please try again.");
                }
            }

            await PopulateParentDropDownAsync(item.ParentId, excludeId: item.Id);
            return View(item);
        }

        // ════════════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════════════

        /// <summary>GET /Items/Delete/5  – confirmation page</summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();

            var item = await _context.Items
                                     .Include(i => i.Parent)
                                     .Include(i => i.Children)
                                     .FirstOrDefaultAsync(i => i.Id == id);
            if (item is null) return NotFound();

            return View(item);
        }

        /// <summary>POST /Items/Delete/5 – actual deletion</summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items
                                     .Include(i => i.Children)
                                     .FirstOrDefaultAsync(i => i.Id == id);
            if (item is null) return NotFound();

            if (item.Children.Any())
            {
                TempData["Error"] = "Cannot delete an item that has children. " +
                                    "Please remove or re-parent the children first.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Item \"{item.Name}\" deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {Id}.", id);
                TempData["Error"] = "An error occurred while deleting the item.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ════════════════════════════════════════════════════════════════
        // ADD CHILD  –  attach an existing item as a child of another
        // ════════════════════════════════════════════════════════════════

        /// <summary>GET /Items/AddChild/5  – form to pick which item becomes a child of item 5</summary>
        public async Task<IActionResult> AddChild(int? id)
        {
            if (id is null) return NotFound();

            var parent = await _context.Items.FindAsync(id);
            if (parent is null) return NotFound();

            ViewBag.ParentItem = parent;

            // Candidates = items that are NOT already the parent,
            // NOT an ancestor of the parent (circular guard), and NOT already this parent's child
            var allItems = await _context.Items.ToListAsync();
            var ancestors = await GetAncestorIdsAsync(parent.Id);
            var existingChildIds = await _context.Items
                                                 .Where(i => i.ParentId == parent.Id)
                                                 .Select(i => i.Id)
                                                 .ToListAsync();

            var candidates = allItems
                .Where(i => i.Id != parent.Id
                         && !ancestors.Contains(i.Id)
                         && !existingChildIds.Contains(i.Id))
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem(i.Name, i.Id.ToString()))
                .ToList();

            ViewBag.Candidates = candidates;
            return View();
        }

        /// <summary>POST /Items/AddChild/5</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChild(int id, int childId)
        {
            var parent = await _context.Items.FindAsync(id);
            var child  = await _context.Items.FindAsync(childId);

            if (parent is null || child is null) return NotFound();

            // Circular reference check
            if (await IsCircularAsync(childId, id))
            {
                TempData["Error"] = "Cannot add child: it would create a circular reference.";
                return RedirectToAction(nameof(AddChild), new { id });
            }

            child.ParentId = id;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{child.Name}\" is now a child of \"{parent.Name}\".";
            return RedirectToAction(nameof(Tree));
        }

        // ════════════════════════════════════════════════════════════════
        // PROCESS ITEM  – create one or multiple output child items
        // ════════════════════════════════════════════════════════════════

        /// <summary>GET /Items/Process</summary>
        public async Task<IActionResult> Process()
        {
            await PopulateParentDropDownAsync();

            // Start with a single output row for UX
            return View(new ProcessItemViewModel
            {
                Outputs = new List<ProcessOutputItemViewModel>
                {
                    new ProcessOutputItemViewModel()
                }
            });
        }

        /// <summary>POST /Items/Process</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(ProcessItemViewModel model)
        {
            await PopulateParentDropDownAsync(model.ParentId);

            if (!ModelState.IsValid)
            {
                // Keep the same number of rows the user submitted (validation messages show inline).
                return View(model);
            }

            var parent = await _context.Items.FindAsync(model.ParentId);
            if (parent is null)
            {
                ModelState.AddModelError(nameof(model.ParentId), "Selected parent item does not exist.");
                return View(model);
            }

            if (model.Outputs is null || model.Outputs.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Outputs), "At least one output item is required.");
                return View(model);
            }

            try
            {
                // Create new output items as direct children of the selected parent.
                foreach (var output in model.Outputs)
                {
                    var child = new Item
                    {
                        Name = (output.Name ?? string.Empty).Trim(),
                        Weight = output.Weight,
                        ParentId = model.ParentId
                    };

                    _context.Items.Add(child);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Processed \"{parent.Name}\" and created {model.Outputs.Count} output item(s).";
                return RedirectToAction(nameof(Details), new { id = parent.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing item {ParentId}.", model.ParentId);
                TempData["Error"] = "An error occurred while processing the item.";
                return View(model);
            }
        }

        // ════════════════════════════════════════════════════════════════
        // PROCESSED ITEMS  – items that already have children
        // ════════════════════════════════════════════════════════════════

        /// <summary>GET /Items/Processed</summary>
        public async Task<IActionResult> Processed(string? search)
        {
            ViewData["CurrentSearch"] = search;

            IQueryable<Item> query = _context.Items
                                              .Where(i => i.Children.Any())
                                              .Include(i => i.Parent)
                                              .Include(i => i.Children)
                                              .OrderBy(i => i.Name);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.Name.Contains(search));

            var items = await query.ToListAsync();
            return View(items);
        }

        // ════════════════════════════════════════════════════════════════
        // TREE VIEW  –  recursive hierarchy
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// GET /Items/Tree
        /// Loads all root items (ParentId == null) and recursively builds the tree.
        /// </summary>
        public async Task<IActionResult> Tree()
        {
            // Load ALL items in a single query, then build the tree in memory
            var allItems = await _context.Items
                                         .AsNoTracking()
                                         .ToListAsync();

            // Build a lookup: ParentId -> children (exclude roots to avoid null-key dictionary)
            var childrenMap = allItems
                .Where(i => i.ParentId.HasValue)
                .GroupBy(i => i.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Roots have ParentId == null
            var roots = allItems
                .Where(i => !i.ParentId.HasValue)
                .ToList();

            // Recursively convert to tree-node model
            var treeRoots = roots
                .OrderBy(i => i.Name)
                .Select(i => BuildNode(i, childrenMap, level: 0, visited: new HashSet<int>()))
                .ToList();

            return View(treeRoots);
        }

        // ════════════════════════════════════════════════════════════════
        // DETAILS  –  view single item
        // ════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var item = await _context.Items
                                     .Include(i => i.Parent)
                                     .Include(i => i.Children)
                                     .FirstOrDefaultAsync(i => i.Id == id);
            if (item is null) return NotFound();

            return View(item);
        }

        // ════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Recursively builds an ItemTreeNode from a flat item list (already grouped by ParentId).
        /// The <paramref name="visited"/> set prevents infinite loops on corrupted data.
        /// </summary>
        private static ItemTreeNode BuildNode(
            Item item,
            Dictionary<int, List<Item>> childrenMap,
            int level,
            HashSet<int> visited)
        {
            visited.Add(item.Id);

            var node = new ItemTreeNode { Item = item, Level = level };

            if (childrenMap.TryGetValue(item.Id, out var children))
            {
                foreach (var child in children.OrderBy(c => c.Name))
                {
                    // Skip already-visited nodes (circular reference guard)
                    if (visited.Contains(child.Id)) continue;

                    node.Children.Add(BuildNode(child, childrenMap, level + 1, new HashSet<int>(visited)));
                }
            }

            return node;
        }

        /// <summary>
        /// Returns true if making <paramref name="proposedParentId"/> the parent of
        /// <paramref name="itemId"/> would create a circular reference.
        /// </summary>
        private async Task<bool> IsCircularAsync(int itemId, int proposedParentId)
        {
            // Walk UP from proposedParentId; if we ever reach itemId, it's circular.
            var current = await _context.Items.FindAsync(proposedParentId);
            var visited = new HashSet<int>();

            while (current is not null)
            {
                if (current.Id == itemId) return true;
                if (!visited.Add(current.Id)) break; // safety guard

                current = current.ParentId.HasValue
                    ? await _context.Items.FindAsync(current.ParentId.Value)
                    : null;
            }

            return false;
        }

        /// <summary>Collects all ancestor IDs of <paramref name="itemId"/>.</summary>
        private async Task<HashSet<int>> GetAncestorIdsAsync(int itemId)
        {
            var ids = new HashSet<int>();
            var current = await _context.Items.FindAsync(itemId);

            while (current?.ParentId is not null)
            {
                if (!ids.Add(current.ParentId.Value)) break; // circular safety
                current = await _context.Items.FindAsync(current.ParentId.Value);
            }

            return ids;
        }

        /// <summary>Populates ViewBag.ParentList for the parent &lt;select&gt; dropdown.</summary>
        private async Task PopulateParentDropDownAsync(int? selectedId = null, int? excludeId = null)
        {
            var items = await _context.Items
                                      .Where(i => excludeId == null || i.Id != excludeId)
                                      .OrderBy(i => i.Name)
                                      .ToListAsync();

            ViewBag.ParentId = new SelectList(items, "Id", "Name", selectedId);
        }

        private bool ItemExists(int id) =>
            _context.Items.Any(e => e.Id == id);
    }
}
