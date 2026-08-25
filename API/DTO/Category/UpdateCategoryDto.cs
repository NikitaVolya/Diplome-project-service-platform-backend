using System.ComponentModel.DataAnnotations;

namespace API.DTO.Category
{
    public class UpdateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; }
    }
}
