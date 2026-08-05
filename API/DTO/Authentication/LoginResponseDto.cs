namespace API.DTO.Authentication
{
    public class LoginResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime ExpiresAt { get; init; }

        public UserResponseDto User { get; init; } = null!;
    }
}
