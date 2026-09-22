namespace BLL.Public
{
    // Категорія у блоці «Популярні категорії» на головній сторінці.
    public class PublicCategoryItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        // Шлях до SVG-іконки у wwwroot; підбирається за назвою категорії. 
        public string Icon { get; set; } = "/img/icons/cat-more.svg";
    }

    // Картка у блоці «Популярне поруч із вами»: напрям послуг разом із рейтингом і мінімальною ціною.
    public class PublicServiceItem
    {
        public int CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal FromPrice { get; set; }

        public double Rating { get; set; }

        public int ReviewsCount { get; set; }

        // Шлях до зображення картки у wwwroot. 
        public string Image { get; set; } = "/img/landing/service-1.jpg";
    }
}
