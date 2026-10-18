using API.DTO.Admin;
using AutoMapper;
using BLL.Services.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admins")]
    [Authorize(Roles = nameof(UserRole.SuperAdmin))]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IMapper _mapper;

        public AdminController(
            IAdminService adminService, 
            IMapper mapper)
        {
            _adminService = adminService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminResponseDto>>> GetAll()
        {
            var admins = await _adminService.GetAllAsync();

            return Ok(_mapper.Map<IEnumerable<AdminResponseDto>>(admins));
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<AdminResponseDto>> GetById(
            string userId)
        {
            var admin = await _adminService.GetByIdAsync(userId);

            if (admin is null)
                return NotFound();

            return Ok(_mapper.Map<AdminResponseDto>(admin));
        }

        [HttpPost]
        public async Task<ActionResult<AdminResponseDto>> Add(
            AddAdminRequestDto request)
        {
            try
            {
                var admin = await _adminService.AddAsync(
                    request.UserId);

                return Ok(_mapper.Map<AdminResponseDto>(admin));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> Remove(
            string userId)
        {
            var removed = await _adminService.RemoveAsync(userId);

            if (!removed)
                return NotFound();

            return NoContent();
        }
    }
}
