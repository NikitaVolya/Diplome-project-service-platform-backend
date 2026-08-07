using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? IconUrl { get; set; }

        public int? ParentCategoryId { get; set; }

        [ForeignKey("ParentCategoryId")]
        public Category ParentCategory { get; set; }

        [InverseProperty("ParentCategory")]
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();

        [InverseProperty("Category")]
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public bool IsActive { get; set; } = true;
    }
}
