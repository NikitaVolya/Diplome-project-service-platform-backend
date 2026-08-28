using System.Security.Claims;
using BLL.Admin.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AdminPanel.Hubs
{
    /// <summary>
    /// Живий чат підтримки. Діалог кожного замовлення — це група SignalR з іменем «order-{id}»,
    /// тому повідомлення отримують лише адміністратори, які зараз відкрили цю розмову, а коротке
    /// сповіщення йде всьому персоналу через спільну групу «staff».
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
        /// Спочатку зберігає повідомлення, а розсилає вже те, що реально записано, —
        /// щоб у всіх клієнтів були той самий ідентифікатор і час, що й у базі.
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

        // Індикатор набору тексту; свідомо не зберігається в базі. 
        public Task Typing(int orderId)
        {
            return Clients.OthersInGroup(GroupOf(orderId))
                .SendAsync("UserTyping", Context.User?.Identity?.Name ?? "Support");
        }

        private static string GroupOf(int orderId) => $"order-{orderId}";

        private string? CurrentUserId() => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
