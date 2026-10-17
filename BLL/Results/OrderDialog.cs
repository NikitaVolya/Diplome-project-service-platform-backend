using Domain.Entities;
using Domain.Models;

namespace BLL.Results
{
    /// <summary>
    /// One conversation in the dialog list: ONE order and ONE pair of participants.
    /// The same order can appear several times for a customer, once per executor he talks to.
    /// </summary>
    public sealed class OrderDialog
    {
        public Order Order { get; init; } = null!;

        public ApplicationUser Customer { get; init; } = null!;

        /// <summary>The executor of THIS conversation (not necessarily the one assigned to the order).</summary>
        public ApplicationUser Executor { get; init; } = null!;

        public OrderMessage LastMessage { get; init; } = null!;

        /// <summary>Messages of this conversation that the current user has not read yet.</summary>
        public int UnreadCount { get; init; }
    }
}
