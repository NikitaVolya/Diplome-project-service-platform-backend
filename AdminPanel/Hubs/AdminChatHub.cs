using System.Security.Claims;
using BLL.Admin.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AdminPanel.Hubs
{
    /// <summary>
    /// Live support chat. Each order dialog is a SignalR group named "order-{id}", so a message is
    /// delivered only to the administrators currently looking at that conversation, while a short
    /// notice goes to every signed-in staff member through the shared "staff" group.
    /// </summary>
    [Authorize(Policy = AppRoles.StaffPolicy)]
    public class AdminChatHub : Hub
    {
        public const string StaffGroup = "staff";

        private readonly IAdminChatService _chatService;
        private readonly ILogger<AdminChatHub> _logger;

        public AdminChatHub(IAdminChatService chatService, ILogger<AdminChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, StaffGroup);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinDialog(int orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupOf(orderId));

            var userId = CurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _chatService.MarkReadAsync(orderId, userId);
            }
        }

        public async Task LeaveDialog(int orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupOf(orderId));
        }

        /// <summary>
        /// Persists the message first and broadcasts what was actually stored, so every client shows
        /// the same id and timestamp as the database.
        /// </summary>
        public async Task SendMessage(int orderId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Your session has expired. Please sign in again.");
            }

            try
            {
                var message = await _chatService.SendAsync(orderId, userId, text);

                await Clients.Group(GroupOf(orderId)).SendAsync("ReceiveMessage", new
                {
                    message.Id,
                    message.OrderId,
                    message.SenderId,
                    message.SenderName,
                    message.Text,
                    SentAt = message.SentAt.ToString("O"),
                    IsStaff = true
                });

                await Clients.Group(StaffGroup).SendAsync("DialogUpdated", new
                {
                    message.OrderId,
                    Preview = message.Text.Length > 60 ? message.Text[..60] + "…" : message.Text,
                    SentAt = message.SentAt.ToString("O")
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Chat message rejected for order {OrderId}", orderId);
                throw new HubException(ex.Message);
            }
        }

        /// <summary>Typing indicator; deliberately not persisted.</summary>
        public Task Typing(int orderId)
        {
            return Clients.OthersInGroup(GroupOf(orderId))
                .SendAsync("UserTyping", Context.User?.Identity?.Name ?? "Support");
        }

        private static string GroupOf(int orderId) => $"order-{orderId}";

        private string? CurrentUserId() => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
