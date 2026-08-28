using AdminPanel.Models;
using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    /// <summary>
    /// Екран «що вимагає уваги просто зараз»: усе, що чекає на рішення, зібрано в одному місці,
    /// щоб модератор не обходив чотири різні списки в пошуках роботи.
    /// </summary>
    [Authorize(Policy = AppRoles.ModerationPolicy)]
    public class ModerationController : AdminControllerBase
    {
        private const int QueuePageSize = 10;

        private readonly IAdminModerationService _moderation;

        public ModerationController(IAdminModerationService moderation)
        {
            _moderation = moderation;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var complaints = await _moderation.GetComplaintsAsync(
                new ComplaintFilter
                {
                    Status = ComplaintStatus.Pending,
                    PageSize = QueuePageSize,
                    SortDesc = false // oldest first: the longest-waiting complaint is the most urgent
                },
                cancellationToken);

            var reviews = await _moderation.GetReviewsAsync(
                new ReviewFilter
                {
                    MaxRating = 2,
                    PageSize = QueuePageSize
                },
                cancellationToken);

            var model = new ModerationViewModel
            {
                Queue = await _moderation.GetQueueAsync(cancellationToken),
                PendingComplaints = complaints.Items,
                LowRatedReviews = reviews.Items
            };

            return View(model);
        }
    }
}
