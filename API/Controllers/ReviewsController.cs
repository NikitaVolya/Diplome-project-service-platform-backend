using API.DTO.Review;
using AutoMapper;
using BLL.Services.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly IMapper _mapper;

        public ReviewsController(IReviewService reviewService, IMapper mapper)
        {
            _reviewService = reviewService;
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

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var review = await _reviewService.GetByIdAsync(id);
            if (review == null)
            {
                return NotFound(new { message = $"Review with ID {id} not found." });
            }
            return Ok(_mapper.Map<ReviewResponseDto>(review));
        }

        [HttpGet("order/{orderId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var review = await _reviewService.GetByOrderIdAsync(orderId);
            if (review == null)
            {
                return NotFound(new { message = $"No review found for order ID {orderId}." });
            }
            return Ok(_mapper.Map<ReviewResponseDto>(review));
        }

        [HttpGet("user/{targetUserId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserReviews(string targetUserId)
        {
            try
            {
                var reviews = await _reviewService.GetUserReviewsAsync(targetUserId);
                return Ok(_mapper.Map<IEnumerable<ReviewResponseDto>>(reviews));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("authored/{authorId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAuthoredReviews(string authorId)
        {
            try
            {
                var reviews = await _reviewService.GetAuthoredReviewsAsync(authorId);
                return Ok(_mapper.Map<IEnumerable<ReviewResponseDto>>(reviews));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("rating/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAverageRating(string userId)
        {
            var rating = await _reviewService.GetAverageRatingAsync(userId);
            return Ok(new { userId, averageRating = rating });
        }

        [HttpPost]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var review = _mapper.Map<Review>(dto);
                review.AuthorId = userId;

                var createdReview = await _reviewService.CreateReviewAsync(review);
                var responseDto = _mapper.Map<ReviewResponseDto>(createdReview);

                return CreatedAtAction(nameof(GetById), new { id = createdReview.Id }, responseDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var reviewToUpdate = new Review
                {
                    Id = id,
                    Rating = dto.Rating,
                    Comment = dto.Comment
                };

                await _reviewService.UpdateReviewAsync(reviewToUpdate, userId);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Customer,Executor,Admin")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _reviewService.DeleteReviewAsync(id, userId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }
    }
}