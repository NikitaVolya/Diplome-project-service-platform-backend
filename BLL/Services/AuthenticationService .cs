using BLL.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using BLL.Results;


namespace BLL.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;


        public AuthenticationService(UserManager<ApplicationUser> userManager, IJwtService jwtService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
        }

        public async Task<ApplicationUser> RegisterAsync(
            string email,
            string username,
            string password,
            string firstName,
            string lastName)
        {

            var user = new ApplicationUser
            {
                Email = email,
                UserName = username,
                FirstName = firstName,
                LastName = lastName
            };


            var result = await _userManager.CreateAsync(user, password);


            if (!result.Succeeded)
            {
                throw new Exception(
                    string.Join(
                        ", ",
                        result.Errors.Select(x => x.Description)
                    ));
            }

            return user;
        }



        public async Task<AuthenticationResult?> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return null;

            if (!await _userManager.CheckPasswordAsync(user, password))
                return null;

            var accessToken =
                await _jwtService.GenerateAccessTokenAsync(user);

            return new AuthenticationResult
            {
                User = user,
                AccessToken = accessToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };
        }
    }
}
