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
    }
}
