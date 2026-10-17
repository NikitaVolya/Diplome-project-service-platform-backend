using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Complaint
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public ApplicationUser Sender { get; set; } = null!;

        public string? TargetUserId { get; set; }
        public ApplicationUser? TargetUser { get; set; }

        public string Reason { get; set; }
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
    }

    public enum ComplaintStatus
    {
        Pending,
        Resolved,
        Rejected
    }
}
