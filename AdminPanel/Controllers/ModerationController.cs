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
    /// The "what needs my attention right now" screen: everything queued for a decision in one place,
    /// so a moderator does not have to walk through four separate lists to find open work.
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
