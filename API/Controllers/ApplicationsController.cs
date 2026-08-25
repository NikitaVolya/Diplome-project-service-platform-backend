using API.DTO.Application;
using AutoMapper;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationService _applicationService;
        private readonly IMapper _mapper;

        public ApplicationsController(IApplicationService applicationService, IMapper mapper)
        {
            _applicationService = applicationService;
            _mapper = mapper;
        }

        private string GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return userId;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var application = await _applicationService.GetByIdAsync(id);
            if (application == null)
            {
                return NotFound(new { message = $"Application with id {id} not found." });
            }

            var responseDto = _mapper.Map<ApplicationResponseDto>(application);
            return Ok(responseDto);
        }

        [HttpGet("order/{orderId:int}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var applications = await _applicationService.GetByOrderIdAsync(orderId);
            var dtos = _mapper.Map<IEnumerable<ApplicationResponseDto>>(applications);
            return Ok(dtos);
        }

        [HttpGet("my-applications")]
        [Authorize(Roles = "Executor")]
        public async Task<IActionResult> GetMyApplications()
        {
            var currentUserId = GetCurrentUserId();
            var applications = await _applicationService.GetByExecutorIdAsync(currentUserId);
            var dtos = _mapper.Map<IEnumerable<ApplicationResponseDto>>(applications);
            return Ok(dtos);
        }

        [HttpPost]
        [Authorize(Roles = "Executor")]
        public async Task<IActionResult> Create([FromBody] CreateApplicationDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var application = await _applicationService.CreateAsync(
                    dto.OrderId,
                    currentUserId,
                    dto.ProposedPrice,
                    dto.Comment
                );

                var responseDto = _mapper.Map<ApplicationResponseDto>(application);
                return CreatedAtAction(nameof(GetById), new { id = application.Id }, responseDto);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/accept")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Accept(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _applicationService.AcceptApplicationAsync(id, currentUserId);
                return Ok(new { message = "Application accepted successfully." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/reject")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _applicationService.RejectApplicationAsync(id, currentUserId);
                return Ok(new { message = "Application rejected successfully." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Executor,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _applicationService.DeleteAsync(id, currentUserId);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}