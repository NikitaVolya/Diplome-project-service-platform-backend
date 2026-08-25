using API.DTO.Statistic;
using AutoMapper;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticService _statisticService;
        private readonly IMapper _mapper;

        public StatisticsController(IStatisticService statisticService, IMapper mapper)
        {
            _statisticService = statisticService;
            _mapper = mapper;
        }

        private string GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return userId;
        }

        [HttpGet("my-stats")]
        [Authorize(Roles = "Executor")]
        public async Task<IActionResult> GetMyStatistic([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var userId = GetCurrentUserId();
                var statistic = await _statisticService.GetMasterStatisticAsync(userId, startDate, endDate);
                return Ok(_mapper.Map<StatisticResponseDto>(statistic));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("date/{date:datetime}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByDate(DateTime date)
        {
            var statistic = await _statisticService.GetByDateAsync(date);
            if (statistic == null)
            {
                return NotFound(new { message = $"No statistics found for date {date:yyyy-MM-dd}." });
            }
            return Ok(_mapper.Map<StatisticResponseDto>(statistic));
        }

        [HttpGet("latest")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLatest()
        {
            var statistic = await _statisticService.GetLatestAsync();
            if (statistic == null)
            {
                return NotFound(new { message = "No statistics available." });
            }
            return Ok(_mapper.Map<StatisticResponseDto>(statistic));
        }

        [HttpGet("range")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateRangeDto rangeDto)
        {
            try
            {
                var statistics = await _statisticService.GetByDateRangeAsync(rangeDto.StartDate, rangeDto.EndDate);
                return Ok(_mapper.Map<IEnumerable<StatisticResponseDto>>(statistics));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("aggregate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AggregatePeriodStatistic([FromQuery] DateRangeDto rangeDto)
        {
            try
            {
                var aggregatedStatistic = await _statisticService.AggregatePeriodStatisticAsync(rangeDto.StartDate, rangeDto.EndDate);
                return Ok(_mapper.Map<StatisticResponseDto>(aggregatedStatistic));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("recalculate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecalculateDailyStatistic([FromQuery] DateTime date)
        {
            var updatedStatistic = await _statisticService.RecalculateDailyStatisticAsync(date);
            return Ok(_mapper.Map<StatisticResponseDto>(updatedStatistic));
        }
    }
}