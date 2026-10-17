using API.DTO.Authentication;
using API.DTO.User;
using AutoMapper;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


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
        public async Task<ActionResult<UserMeResponseDto>> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userService.GetByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return NotFound(new { message = "User not found." });

            var dto = _mapper.Map<UserMeResponseDto>(user);

            dto.Roles = (await _userService.GetRolesAsync(userId)).ToList();

            return Ok(dto);
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

        [HttpPost("become-executer")]
        [Authorize]
        public async Task<IActionResult> BecomeExecuter()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _userService.BecomeExecuterAsync(userId);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
