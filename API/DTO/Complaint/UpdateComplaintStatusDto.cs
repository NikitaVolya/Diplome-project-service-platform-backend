using Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace API.DTO.Complaint
{
    public class UpdateComplaintStatusDto
    {
        [Required(ErrorMessage = "New status is required.")]
        public ComplaintStatus Status { get; set; }
    }
}