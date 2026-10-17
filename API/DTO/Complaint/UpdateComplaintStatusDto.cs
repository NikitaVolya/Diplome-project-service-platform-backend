using Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace API.DTO.Complaint
{
    public class UpdateComplaintStatusDto
    {
        public ComplaintStatus Status { get; set; }
    }
}