namespace ItemProcessingApp.Models
{
    /// <summary>
    /// Wraps an Item together with its recursively-resolved children,
    /// used by the Tree view to render the hierarchy.
    /// </summary>
    public class ItemTreeNode
    {
        public Item Item { get; set; } = null!;

        /// <summary>Depth in the tree (0 = root).</summary>
        public int Level { get; set; }

        /// <summary>Resolved child nodes (already sorted and populated).</summary>
        public List<ItemTreeNode> Children { get; set; } = new();
    }
}
