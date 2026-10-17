namespace API.DTO.Favorite
{
    public class FavoriteOrderResponseDto
    {
        public int Id { get; set; }
        public int TargetOrderId { get; set; }
        public string? OrderTitle { get; set; }
        public decimal? OrderPrice { get; set; }
        public string? CategoryName { get; set; }
    }
}