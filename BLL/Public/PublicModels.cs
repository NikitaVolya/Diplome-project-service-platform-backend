namespace BLL.Public
{
    /// <summary>
    /// Категорія у блоці «Популярні категорії» на головній сторінці.
    /// </summary>
    public class PublicCategoryItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Шлях до SVG-іконки у wwwroot; підбирається за назвою категорії.</summary>
        public string Icon { get; set; } = "/img/icons/cat-more.svg";
    }

    /// <summary>
    /// Картка у блоці «Популярне поруч із вами»: напрям послуг разом із рейтингом і мінімальною ціною.
    /// </summary>
    public class PublicServiceItem
    {
        public int CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal FromPrice { get; set; }

        public double Rating { get; set; }

        public int ReviewsCount { get; set; }

        /// <summary>Шлях до зображення картки у wwwroot.</summary>
        public string Image { get; set; } = "/img/landing/service-1.jpg";
    }
}
