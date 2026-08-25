using API.DTO.OrderMessage;
using AutoMapper;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderMessagesController : ControllerBase
    {
        private readonly IOrderMessageService _messageService;
        private readonly IMapper _mapper;

        public OrderMessagesController(IOrderMessageService messageService, IMapper mapper)
        {
            _messageService = messageService;
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

        [HttpPost("orders/{orderId:int}")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> SendMessage(int orderId, [FromBody] SendOrderMessageDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var message = await _messageService.SendMessageAsync(orderId, userId, dto.Text);
                var responseDto = _mapper.Map<OrderMessageResponseDto>(message);
                return Ok(responseDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        [HttpGet("orders/{orderId:int}")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> GetOrderMessages(int orderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var messages = await _messageService.GetOrderMessagesAsync(orderId, userId);
                var responseDtos = _mapper.Map<IEnumerable<OrderMessageResponseDto>>(messages);
                return Ok(responseDtos);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        [HttpGet("dialogs")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> GetUserDialogs()
        {
            var userId = GetCurrentUserId();
            var orders = await _messageService.GetUserDialogsAsync(userId);

            var dialogDtos = _mapper.Map<IEnumerable<OrderDialogResponseDto>>(orders, opts =>
            {
                opts.Items["CurrentUserId"] = userId;
            });

            return Ok(dialogDtos);
        }

        [HttpGet("unread/count")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> GetTotalUnreadCount()
        {
            var userId = GetCurrentUserId();
            var count = await _messageService.GetTotalUnreadCountAsync(userId);
            return Ok(new { unreadCount = count });
        }
    }
}