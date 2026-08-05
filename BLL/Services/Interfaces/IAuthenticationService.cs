using BLL.Results;
using Domain.Entities;


namespace BLL.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<ApplicationUser> RegisterAsync(
            string email,
            string username,
            string password,
            string firstName,
            string lastName);


        Task<AuthenticationResult?> LoginAsync(
            string email,
            string password);
    }
}
