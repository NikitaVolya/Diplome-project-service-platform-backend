namespace BLL.Public
{
    public class PublicCategoryItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Icon { get; set; } = "/img/icons/cat-more.svg";
    }

    public class PublicServiceItem
    {
        public int CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal FromPrice { get; set; }

        public double Rating { get; set; }

        public int ReviewsCount { get; set; }

        public string Parent { get; set; } = string.Empty;

        public string Image { get; set; } = "/img/landing/service-1.jpg";
    }
}
