using Domain.Entities;


namespace BLL.Services.Interfaces
{
    public interface IJwtService
    {
        Task<string> GenerateAccessTokenAsync(ApplicationUser user);
    }
}
