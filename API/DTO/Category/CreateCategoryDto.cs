using System.ComponentModel.DataAnnotations;

namespace API.DTO.Category
{
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int? ParentCategoryId { get; set; } = null;
        public bool IsActive { get; set; } = true;
    }
}
