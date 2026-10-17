using API.DTO.Order;
using AutoMapper;
using BLL.Exceptions;
using BLL.Services.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private const int MaxPageSize = 100;

        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;
        private readonly IGoogleMapsService _googleMapsService;

        public OrdersController(
            IOrderService orderService,
            IMapper mapper,
            IGoogleMapsService googleMapsService)
        {
            _orderService = orderService;
            _mapper = mapper;
            // Injected through DI (typed HttpClient + IConfiguration + ILogger).
            // Previously this was built by hand with new HttpClient(), a freshly parsed
            // appsettings.json and a null logger.
            _googleMapsService = googleMapsService;
        }

        private string GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException("User is not authenticated.");
            }
            return userId;
        }

        // Fills DistanceKm / DurationMinutes of the list items with ONE batched Distance Matrix call
        // (25 destinations per request) instead of one call per order.
        private async Task FillRouteDetailsAsync(
            IReadOnlyList<Order> orders,
            IReadOnlyList<OrderResponseDto> dtos,
            double originLat,
            double originLng,
            CancellationToken cancellationToken)
        {
            var dtoIndexes = new List<int>();
            var destinations = new List<(double Lat, double Lng)>();

            for (var i = 0; i < orders.Count; i++)
            {
                // (0, 0) means "the address could not be geocoded": there is nothing meaningful to measure.
                if (orders[i].Latitude == 0 && orders[i].Longitude == 0)
                {
                    continue;
                }

                dtoIndexes.Add(i);
                destinations.Add((orders[i].Latitude, orders[i].Longitude));
            }

            if (destinations.Count == 0)
            {
                return;
            }

            var routes = await _googleMapsService.GetRouteDetailsBatchAsync(
                originLat,
                originLng,
                destinations,
                cancellationToken);

            for (var k = 0; k < dtoIndexes.Count; k++)
            {
                var route = routes[k];
                if (route.HasValue)
                {
                    dtos[dtoIndexes[k]].DistanceKm = route.Value.DistanceKm;
                    dtos[dtoIndexes[k]].DurationMinutes = route.Value.DurationMinutes;
                }
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOrderById(
            int id,
            [FromQuery] double? currentLat = null,
            [FromQuery] double? currentLng = null)
        {
            var order = await _orderService.GetWithDetailsByIdAsync(id);
            if (order == null)
            {
                throw new NotFoundException("Order not found.");
            }

            var responseDto = _mapper.Map<OrderResponseDto>(order);

            // Distance Matrix is a paid Google API: only signed-in users may trigger it,
            // otherwise anonymous callers could burn the quota.
            if (User.Identity?.IsAuthenticated == true && currentLat.HasValue && currentLng.HasValue)
            {
                var routeDetails = await _googleMapsService.GetRouteDetailsAsync(
                    currentLat.Value,
                    currentLng.Value,
                    order.Latitude,
                    order.Longitude);

                if (routeDetails.HasValue)
                {
                    responseDto.DistanceKm = routeDetails.Value.DistanceKm;
                    responseDto.DurationMinutes = routeDetails.Value.DurationMinutes;
                }
            }

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
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            // pageIndex <= 0 produced a negative Skip (SQL error) and a huge pageSize
            // allowed dumping the whole table.
            pageIndex = Math.Max(1, pageIndex);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var (items, totalCount) = await _orderService.GetFilteredOrdersAsync(
                categoryId,
                status,
                searchTerm,
                latitude,
                longitude,
                radiusKm,
                pageIndex,
                pageSize);

            var orders = items.ToList();
            var itemsDto = _mapper.Map<List<OrderResponseDto>>(orders);

            // Same rule as GET /api/Orders/{id}: Distance Matrix is a paid API, so only signed-in users
            // may trigger it, and only when the client sent its own coordinates.
            if (User.Identity?.IsAuthenticated == true && latitude.HasValue && longitude.HasValue)
            {
                await FillRouteDetailsAsync(orders, itemsDto, latitude.Value, longitude.Value, cancellationToken);
            }

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
            var order = _mapper.Map<Order>(dto);
            order.CustomerId = GetCurrentUserId();

            var createdOrder = await _orderService.CreateOrderAsync(order);
            var responseDto = _mapper.Map<OrderResponseDto>(createdOrder);

            return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, responseDto);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var orderToUpdate = _mapper.Map<Order>(dto);
            orderToUpdate.Id = id;

            await _orderService.UpdateOrderAsync(orderToUpdate, currentUserId);
            return NoContent();
        }

        [HttpPatch("{id:int}/complete")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Complete(int id)
        {
            var currentUserId = GetCurrentUserId();
            await _orderService.CompleteOrderAsync(id, currentUserId);
            return Ok(new { message = "Order was successfully completed." });
        }

        [HttpPatch("{id:int}/cancel")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Cancel(int id)
        {
            var currentUserId = GetCurrentUserId();
            await _orderService.CancelOrderAsync(id, currentUserId);
            return Ok(new { message = "Order was successfully cancelled." });
        }

        [HttpPatch("{id:int}/assign-executor")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AssignExecutor(int id, [FromQuery] string executorId)
        {
            var currentUserId = GetCurrentUserId();
            await _orderService.AssignExecutorAsync(id, executorId, currentUserId);
            return Ok(new { message = "Executor was successfully assigned to the order." });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = GetCurrentUserId();
            await _orderService.DeleteOrderAsync(id, currentUserId);
            return NoContent();
        }
    }
}
