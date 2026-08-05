

using Domain.Entities;

namespace BLL.Results
{
    public class AuthenticationResult
    {
        public ApplicationUser User { get; init; } = null!;
        public string AccessToken { get; init; } = null!;
        public DateTime ExpiresAt { get; init; }
    }
}
