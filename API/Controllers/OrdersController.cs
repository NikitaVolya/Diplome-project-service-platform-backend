using API.DTO.Order;
using AutoMapper;
using BLL.Services.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;

        public OrdersController(IOrderService orderService, IMapper mapper)
        {
            _orderService = orderService;
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
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetWithDetailsByIdAsync(id);
            if (order == null)
            {
                return NotFound(new { Message = "Order not found" });
            }

            var responseDto = _mapper.Map<OrderResponseDto>(order);
            return Ok(responseDto);
        }

        [HttpGet("filtered")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFiltered(
            [FromQuery] int? categoryId,
            [FromQuery] OrderStatus? status,
            [FromQuery] string? searchTerm,
            [FromQuery] double? latitude,
            [FromQuery] double? longitude,
            [FromQuery] double? radiusKm,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var (items, totalCount) = await _orderService.GetFilteredOrdersAsync(
                categoryId,
                status,
                searchTerm,
                latitude,
                longitude,
                radiusKm,
                pageIndex,
                pageSize);

            var itemsDto = _mapper.Map<IEnumerable<OrderResponseDto>>(items);

            return Ok(new
            {
                Items = itemsDto,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            });
        }

        [HttpGet("my-created")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyCreatedOrders()
        {
            var currentUserId = GetCurrentUserId();
            var orders = await _orderService.GetCustomerOrdersAsync(currentUserId);
            var dtos = _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
            return Ok(dtos);
        }

        [HttpGet("my-assigned")]
        [Authorize(Roles = "Executor")]
        public async Task<IActionResult> GetMyAssignedOrders()
        {
            var currentUserId = GetCurrentUserId();
            var orders = await _orderService.GetExecutorOrdersAsync(currentUserId);
            var dtos = _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
            return Ok(dtos);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            try
            {
                var order = _mapper.Map<Order>(dto);
                order.CustomerId = GetCurrentUserId();

                var createdOrder = await _orderService.CreateOrderAsync(order);
                var responseDto = _mapper.Map<OrderResponseDto>(createdOrder);

                return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, responseDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var orderToUpdate = _mapper.Map<Order>(dto);
                orderToUpdate.Id = id;

                await _orderService.UpdateOrderAsync(orderToUpdate, currentUserId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/complete")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _orderService.CompleteOrderAsync(id, currentUserId);
                return Ok(new { message = "Order was successfully completed." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/cancel")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _orderService.CancelOrderAsync(id, currentUserId);
                return Ok(new { message = "Order was successfully cancelled." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/assign-executor")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AssignExecutor(int id, [FromQuery] string executorId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _orderService.AssignExecutorAsync(id, executorId, currentUserId);
                return Ok(new { message = "Executor was successfully assigned to the order." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _orderService.DeleteOrderAsync(id, currentUserId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
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