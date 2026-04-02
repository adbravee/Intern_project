using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItemProcessingApp.Models
{
    /// <summary>
    /// Represents an Item that can have a parent and multiple children,
    /// forming a recursive tree (hierarchy) structure.
    /// </summary>
    public class Item
    {
        [Key]
        public int Id { get; set; }

        // ── Name ─────────────────────────────────────────────
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Item Name")]
        public string Name { get; set; } = string.Empty;

        // ── Weight ───────────────────────────────────────────
        [Required(ErrorMessage = "Weight is required.")]
        [Range(0.001, double.MaxValue, ErrorMessage = "Weight must be greater than 0.")]
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Weight (kg)")]
        public decimal Weight { get; set; }

        // ── Self-referencing FK (parent / child) ─────────────
        [Display(Name = "Parent Item")]
        public int? ParentId { get; set; }

        // Navigation: the parent item (null if this is a root item)
        [ForeignKey(nameof(ParentId))]
        public virtual Item? Parent { get; set; }

        // Navigation: all direct children of this item
        public virtual ICollection<Item> Children { get; set; } = new List<Item>();
    }
}
