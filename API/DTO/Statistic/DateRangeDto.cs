using System.ComponentModel.DataAnnotations;

namespace API.DTO.Statistic
{
    public class DateRangeDto
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}