using System.ComponentModel.DataAnnotations;

namespace ItemProcessingApp.Models
{
    /// <summary>
    /// View-model for the "Process Item" screen:
    /// select a parent item (input), then create one or more output child items.
    /// </summary>
    public class ProcessItemViewModel
    {
        [Required(ErrorMessage = "Parent item is required.")]
        [Display(Name = "Parent Item")]
        public int ParentId { get; set; }

        [MinLength(1, ErrorMessage = "At least one output item is required.")]
        [Display(Name = "Output Items")]
        public List<ProcessOutputItemViewModel> Outputs { get; set; } = new();
    }

    public class ProcessOutputItemViewModel
    {
        [Required(ErrorMessage = "Output name is required.")]
        [StringLength(100, ErrorMessage = "Output name cannot exceed 100 characters.")]
        [Display(Name = "Output Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Output weight is required.")]
        [Range(0.001, double.MaxValue, ErrorMessage = "Weight must be greater than 0.")]
        [Display(Name = "Output Weight (kg)")]
        public decimal Weight { get; set; }
    }
}

