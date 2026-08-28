using AdminPanel.Models;
using BLL.Admin.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    /// <summary>
    /// Renders the chat workspace. The live traffic itself goes through <see cref="Hubs.AdminChatHub"/>;
    /// this controller only provides the initial state and a JSON endpoint for switching dialogs.
    /// </summary>
    public class ChatController : AdminControllerBase
    {
        private readonly IAdminChatService _chat;

        public ChatController(IAdminChatService chat)
        {
            _chat = chat;
        }

        public async Task<IActionResult> Index(int? orderId, string? search, CancellationToken cancellationToken)
        {
            var dialogs = await _chat.GetDialogsAsync(search, cancellationToken);
            var selected = orderId.HasValue
                ? dialogs.FirstOrDefault(d => d.OrderId == orderId.Value)
                : dialogs.FirstOrDefault();

            var model = new ChatViewModel
            {
                Dialogs = dialogs,
                Selected = selected,
                Search = search,
                CurrentUserId = CurrentUserId,
                Messages = selected == null
                    ? Array.Empty<BLL.Admin.Models.AdminChatMessage>()
                    : await _chat.GetMessagesAsync(selected.OrderId, cancellationToken)
            };

            if (selected != null)
            {
                await _chat.MarkReadAsync(selected.OrderId, CurrentUserId, cancellationToken);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Messages(int orderId, CancellationToken cancellationToken)
        {
            var messages = await _chat.GetMessagesAsync(orderId, cancellationToken);
            await _chat.MarkReadAsync(orderId, CurrentUserId, cancellationToken);

            return Json(messages.Select(m => new
            {
                m.Id,
                m.SenderId,
                m.SenderName,
                m.Text,
                SentAt = m.SentAt.ToString("O"),
                IsMine = m.SenderId == CurrentUserId
            }));
        }
    }
}
