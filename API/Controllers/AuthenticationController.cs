using API.DTO.Authentication;
using AutoMapper;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IMapper _mapper;

        public AuthenticationController(
            IAuthenticationService authenticationService,
            IMapper mapper)
        {
            _authenticationService = authenticationService;
            _mapper = mapper;
        }


        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
        {
            var result =
                await _authenticationService.LoginAsync(
                    request.Email,
                    request.Password);

            if (result == null)
                return Unauthorized();

            return Ok(_mapper.Map<LoginResponseDto>(result));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] DTO.Authentication.ForgotPasswordRequest request)
        {
            await _authenticationService.ForgotPasswordAsync(request.Email);

            return Ok(new
            {
                message = "If an account with this email exists, " +
                          "a password reset link has been sent."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] DTO.Authentication.ResetPasswordRequest request)
        {
            await _authenticationService.ResetPasswordAsync(
                request.Email,
                request.Token,
                request.NewPassword);

            return Ok(new
            {
                message = "Password has been reset successfully."
            });
        }
    }
}
