using API.DTO.Payment;
using AutoMapper;
using BLL.Services.Interfaces;
using BLL.Services.PaymentGateways;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IMapper _mapper;
        private readonly PaymentGatewayFactory _gatewayFactory;

        public PaymentsController(
            IPaymentService paymentService,
            IMapper mapper,
            PaymentGatewayFactory gatewayFactory)
        {
            _paymentService = paymentService;
            _mapper = mapper;
            _gatewayFactory = gatewayFactory;
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
        [Authorize(Roles = "Customer,Executer,Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
            {
                return NotFound(new { message = $"Payment with ID {id} not found." });
            }

            var userId = GetCurrentUserId();
            if (payment.UserId != userId && !User.IsInRole("Admin"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to this payment." });
            }

            return Ok(_mapper.Map<PaymentResponseDto>(payment));
        }

        [HttpGet("my-payments")]
        [Authorize(Roles = "Customer,Executer")]
        public async Task<IActionResult> GetUserPayments()
        {
            var userId = GetCurrentUserId();
            var payments = await _paymentService.GetUserPaymentsAsync(userId);
            return Ok(_mapper.Map<IEnumerable<PaymentResponseDto>>(payments));
        }

        [HttpGet("order/{orderId:int}")]
        [Authorize(Roles = "Customer,Executer,Admin")]
        public async Task<IActionResult> GetOrderPayments(int orderId)
        {
            var payments = await _paymentService.GetOrderPaymentsAsync(orderId);
            return Ok(_mapper.Map<IEnumerable<PaymentResponseDto>>(payments));
        }

        [HttpGet("order/{orderId:int}/is-paid")]
        [Authorize(Roles = "Customer,Executer,Admin")]
        public async Task<IActionResult> IsOrderPaid(int orderId)
        {
            var isPaid = await _paymentService.IsOrderPaidAsync(orderId);
            return Ok(new { orderId, isPaid });
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var payment = _mapper.Map<Payment>(dto);
                payment.UserId = userId;

                var createdPayment = await _paymentService.CreatePaymentAsync(payment);
                var responseDto = _mapper.Map<PaymentResponseDto>(createdPayment);

                return CreatedAtAction(nameof(GetById), new { id = createdPayment.Id }, responseDto);
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

        [HttpPost("{id:int}/checkout")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateGatewayCheckout(int id, [FromQuery] string providerName)
        {
            try
            {
                var payment = await _paymentService.GetByIdAsync(id);
                if (payment == null)
                {
                    return NotFound(new { message = $"Payment with ID {id} not found." });
                }

                var userId = GetCurrentUserId();
                if (payment.UserId != userId && !User.IsInRole("Admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have access to this payment." });
                }

                var gateway = _gatewayFactory.GetGateway(providerName);

                var returnUrl = "https://mysite.com/checkout/result";
                var serverUrl = "https://mysite.com/api/payments/webhook";

                var paymentData = await gateway.CreatePaymentUrlAsync(payment, returnUrl, serverUrl);

                return Ok(new { success = true, provider = gateway.ProviderName, data = paymentData });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessCallback([FromBody] PaymentCallbackDto dto)
        {
            try
            {
                await _paymentService.ProcessCallbackAsync(dto.ExternalTransactionId, dto.NewStatus!.Value);
                return Ok(new { message = "Payment status updated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/refund")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> RefundPayment(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _paymentService.RefundPaymentAsync(id, userId);
                return Ok(new { message = "Payment refunded successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }
    }
}