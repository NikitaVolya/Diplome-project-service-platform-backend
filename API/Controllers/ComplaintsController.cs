using API.DTO.Complaint;
using AutoMapper;
using BLL.Services.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintService _complaintService;
        private readonly IMapper _mapper;

        public ComplaintsController(IComplaintService complaintService, IMapper mapper)
        {
            _complaintService = complaintService;
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
            var complaint = await _complaintService.GetByIdAsync(id);
            if (complaint == null)
            {
                return NotFound(new { message = $"Complaint with ID {id} not found." });
            }

            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && complaint.SenderId != currentUserId && complaint.TargetUserId != currentUserId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to view this complaint." });
            }

            var dto = _mapper.Map<ComplaintResponseDto>(complaint);
            return Ok(dto);
        }

        [HttpGet("my-sent")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> GetMySentComplaints()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var complaints = await _complaintService.GetMyComplaintsAsync(currentUserId);
                var dtos = _mapper.Map<IEnumerable<ComplaintResponseDto>>(complaints);
                return Ok(dtos);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("against-user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetComplaintsAgainstUser(string userId)
        {
            try
            {
                var complaints = await _complaintService.GetComplaintsAgainstUserAsync(userId);
                var dtos = _mapper.Map<IEnumerable<ComplaintResponseDto>>(complaints);
                return Ok(dtos);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByStatus(ComplaintStatus status)
        {
            var complaints = await _complaintService.GetComplaintsByStatusAsync(status);
            var dtos = _mapper.Map<IEnumerable<ComplaintResponseDto>>(complaints);
            return Ok(dtos);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var complaints = await _complaintService.GetAllComplaintsAsync();
            var dtos = _mapper.Map<IEnumerable<ComplaintResponseDto>>(complaints);
            return Ok(dtos);
        }

        [HttpPost]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> Create([FromBody] CreateComplaintDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var complaint = _mapper.Map<Complaint>(dto);
                complaint.SenderId = currentUserId;

                var createdComplaint = await _complaintService.CreateComplaintAsync(complaint);
                var responseDto = _mapper.Map<ComplaintResponseDto>(createdComplaint);

                return CreatedAtAction(nameof(GetById), new { id = createdComplaint.Id }, responseDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] UpdateComplaintStatusDto dto)
        {
            try
            {
                await _complaintService.ChangeStatusAsync(id, dto.Status);
                return Ok(new { message = "Complaint status updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}