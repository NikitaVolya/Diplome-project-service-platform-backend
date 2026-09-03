using API.DTO.Authentication;
using AutoMapper;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : Controller
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public UserController(
            IAuthenticationService authenticationService,
            IUserService userService,
            IMapper mapper)
        {
            _authenticationService = authenticationService;
            _userService = userService;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(User.Identity?.Name);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserResponseDto>> Register(RegisterRequestDto request)
        {
            if (await _userService.GetByEmailAsync(request.Email) != null)
                return BadRequest("Email is already in use.");

            if (await _userService.GetByUserNameAsync(request.Username) != null)
                return BadRequest("Username is already in use.");

            var user =
                await _authenticationService.RegisterAsync(
                    request.Email,
                    request.Username,
                    request.Password,
                    request.FirstName,
                    request.LastName);

            return Ok(_mapper.Map<UserResponseDto>(user));
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return NoContent();
        }
    }
}
