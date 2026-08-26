using BLL.Results;
using BLL.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;


namespace BLL.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;


        public AuthenticationService(UserManager<ApplicationUser> userManager, IJwtService jwtService, IEmailService emailService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _emailService = emailService;
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

        public async Task ForgotPasswordAsync(string email, string site_link = "https://localhost:3000/reset-password")
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var resetLink =
                site_link +
                $"?email={Uri.EscapeDataString(email)}" +
                $"&token={Uri.EscapeDataString(encodedToken)}";

            await _emailService.SendAsync(
                email,
                "Password reset",
                $"""
                Hello!

                You requested a password reset.

                Reset your password using this link:

                {resetLink}

                If you did not request this, you can ignore this email.
                """);
        }

        public async Task ResetPasswordAsync(
            string email,
            string token,
            string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                throw new InvalidOperationException("Invalid password reset request.");
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

            var result = await _userManager.ResetPasswordAsync(
                user,
                decodedToken,
                newPassword);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
            }
        }
    }
}
