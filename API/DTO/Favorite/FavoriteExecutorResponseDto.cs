namespace API.DTO.Favorite
{
    public class FavoriteExecutorResponseDto
    {
        public int Id { get; set; }
        public string TargetExecutorId { get; set; } = string.Empty;
        public string? ExecutorUserName { get; set; }
        public string? ExecutorFullName { get; set; }
    }
}