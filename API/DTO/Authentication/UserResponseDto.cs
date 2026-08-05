namespace API.DTO.Authentication
{
    public class UserResponseDto
    {
        public string Id { get; init; } = string.Empty;

        public string Username { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string FirstName { get; init; } = string.Empty;

        public string LastName { get; init; } = string.Empty;
    }
}
