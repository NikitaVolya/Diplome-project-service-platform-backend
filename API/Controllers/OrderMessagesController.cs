using API.DTO.OrderMessage;
using AutoMapper;
using BLL.Exceptions;
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
                throw new UnauthorizedException("User is not authenticated.");
            }
            return userId;
        }

        private bool IsExecutor => User.IsInRole("Executor");

        /// <summary>
        /// Sends a message in ONE conversation of the order.
        /// Executor: always writes to the customer of the order.
        /// Customer: body.receiverId is the executor to write to (optional once an executor is assigned).
        /// </summary>
        [HttpPost("orders/{orderId:int}")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> SendMessage(int orderId, [FromBody] SendOrderMessageDto dto)
        {
            var message = await _messageService.SendMessageAsync(
                orderId,
                GetCurrentUserId(),
                IsExecutor,
                dto.ReceiverId,
                dto.Text);

            return Ok(_mapper.Map<OrderMessageResponseDto>(message));
        }

        /// <summary>
        /// Messages of ONE conversation of the order. participantId is the other side:
        /// the executor for a customer (optional once an executor is assigned), ignored/optional for an executor.
        /// Messages of other executors on the same order are never included.
        /// </summary>
        [HttpGet("orders/{orderId:int}")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> GetConversation(int orderId, [FromQuery] string? participantId = null)
        {
            var messages = await _messageService.GetConversationAsync(
                orderId,
                GetCurrentUserId(),
                IsExecutor,
                participantId);

            return Ok(_mapper.Map<IEnumerable<OrderMessageResponseDto>>(messages));
        }

        /// <summary>
        /// All conversations of the current user, one entry per (order, other participant).
        /// Optional ?orderId= narrows the list to one order (e.g. all executors a customer talks to about it).
        /// </summary>
        [HttpGet("dialogs")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> GetUserDialogs([FromQuery] int? orderId = null)
        {
            var userId = GetCurrentUserId();
            var dialogs = await _messageService.GetUserDialogsAsync(userId, orderId);

            var dialogDtos = _mapper.Map<IEnumerable<OrderDialogResponseDto>>(dialogs, opts =>
            {
                opts.Items["CurrentUserId"] = userId;
            });

            return Ok(dialogDtos);
        }

        [HttpGet("unread/count")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> GetTotalUnreadCount()
        {
            var count = await _messageService.GetTotalUnreadCountAsync(GetCurrentUserId());
            return Ok(new { unreadCount = count });
        }
    }
}
