namespace API.DTO.Admin
{
    public class AdminResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
