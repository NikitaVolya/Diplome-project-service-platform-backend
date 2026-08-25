using API.DTO.Favorite;
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
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;
        private readonly IMapper _mapper;

        public FavoritesController(IFavoriteService favoriteService, IMapper mapper)
        {
            _favoriteService = favoriteService;
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

        [HttpGet("orders")]
        [Authorize(Roles = "Executor")]
        public async Task<IActionResult> GetFavoriteOrders()
        {
            var userId = GetCurrentUserId();
            var favorites = await _favoriteService.GetFavoriteOrdersAsync(userId);
            var dtos = _mapper.Map<IEnumerable<FavoriteOrderResponseDto>>(favorites);
            return Ok(dtos);
        }

        [HttpGet("executors")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetFavoriteExecutors()
        {
            var userId = GetCurrentUserId();
            var favorites = await _favoriteService.GetFavoriteExecutorsAsync(userId);
            var dtos = _mapper.Map<IEnumerable<FavoriteExecutorResponseDto>>(favorites);
            return Ok(dtos);
        }

        [HttpGet("orders/{orderId:int}/check")]
        [Authorize(Roles = "Executor")]
        public async Task<IActionResult> IsOrderFavorite(int orderId)
        {
            var userId = GetCurrentUserId();
            var isFavorite = await _favoriteService.IsOrderFavoriteAsync(userId, orderId);
            return Ok(new { orderId, isFavorite });
        }

        [HttpGet("executors/{executorId}/check")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> IsExecutorFavorite(string executorId)
        {
            var userId = GetCurrentUserId();
            var isFavorite = await _favoriteService.IsExecutorFavoriteAsync(userId, executorId);
            return Ok(new { executorId, isFavorite });
        }

        [HttpPost("orders/{orderId:int}/toggle")]
        [Authorize(Roles = "Executor")]
        public async Task<IActionResult> ToggleFavoriteOrder(int orderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isFavorite = await _favoriteService.ToggleFavoriteOrderAsync(userId, orderId);
                return Ok(new { orderId, isFavorite, message = isFavorite ? "Order added to favorites." : "Order removed from favorites." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("executors/{executorId}/toggle")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ToggleFavoriteExecutor(string executorId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isFavorite = await _favoriteService.ToggleFavoriteExecutorAsync(userId, executorId);
                return Ok(new { executorId, isFavorite, message = isFavorite ? "Executor added to favorites." : "Executor removed from favorites." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("orders/{orderId:int}")]
        [Authorize(Roles = "Executor")]
        public async Task<IActionResult> RemoveOrderFromFavorites(int orderId)
        {
            var userId = GetCurrentUserId();
            await _favoriteService.RemoveOrderFromFavoritesAsync(userId, orderId);
            return NoContent();
        }

        [HttpDelete("executors/{executorId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RemoveExecutorFromFavorites(string executorId)
        {
            var userId = GetCurrentUserId();
            await _favoriteService.RemoveExecutorFromFavoritesAsync(userId, executorId);
            return NoContent();
        }
    }
}