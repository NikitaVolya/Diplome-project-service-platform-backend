using System.ComponentModel.DataAnnotations;

namespace API.DTO.Statistic
{
    public class DateRangeDto
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}