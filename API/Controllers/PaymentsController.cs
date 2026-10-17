using API.DTO.Payment;
using AutoMapper;
using BLL.Exceptions;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService,
            IMapper mapper,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _mapper = mapper;
            _logger = logger;
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

        private bool IsAdmin => User.IsInRole("Admin");

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Customer,Executor,Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var payment = await _paymentService.GetForUserAsync(id, GetCurrentUserId(), IsAdmin);
            return Ok(_mapper.Map<PaymentResponseDto>(payment));
        }

        [HttpGet("my-payments")]
        [Authorize(Roles = "Customer,Executor")]
        public async Task<IActionResult> GetUserPayments()
        {
            var payments = await _paymentService.GetUserPaymentsAsync(GetCurrentUserId());
            return Ok(_mapper.Map<IEnumerable<PaymentResponseDto>>(payments));
        }

        [HttpGet("order/{orderId:int}")]
        [Authorize(Roles = "Customer,Executor,Admin")]
        public async Task<IActionResult> GetOrderPayments(int orderId)
        {
            var payments = await _paymentService.GetOrderPaymentsAsync(orderId, GetCurrentUserId(), IsAdmin);
            return Ok(_mapper.Map<IEnumerable<PaymentResponseDto>>(payments));
        }

        [HttpGet("order/{orderId:int}/is-paid")]
        [Authorize(Roles = "Customer,Executor,Admin")]
        public async Task<IActionResult> IsOrderPaid(int orderId)
        {
            var isPaid = await _paymentService.IsOrderPaidAsync(orderId, GetCurrentUserId(), IsAdmin);
            return Ok(new { orderId, isPaid });
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            var payment = await _paymentService.CreatePaymentAsync(dto.OrderId, GetCurrentUserId());
            var responseDto = _mapper.Map<PaymentResponseDto>(payment);

            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, responseDto);
        }

        [HttpPost("{id:int}/checkout")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateGatewayCheckout(
            int id,
            [FromQuery] string providerName,
            CancellationToken cancellationToken)
        {
            var checkout = await _paymentService.CreateCheckoutAsync(
                id,
                providerName,
                GetCurrentUserId(),
                cancellationToken);

            // Stripe: redirect the customer to checkout.redirectUrl.
            // LiqPay / WayForPay: POST checkout.formFields to checkout.formAction.
            return Ok(checkout);
        }

        /// <summary>
        /// Single entry point for all payment providers: POST /api/payments/webhook/{stripe|liqpay|wayforpay}.
        /// Anonymous by nature, therefore NOTHING in the request is trusted until the provider's
        /// signature has been verified inside the gateway.
        /// </summary>
        [HttpPost("webhook/{provider}")]
        [AllowAnonymous]
        [RequestSizeLimit(65536)]
        public async Task<IActionResult> Webhook(string provider, CancellationToken cancellationToken)
        {
            var request = await ReadWebhookRequestAsync(cancellationToken);

            // Sizes only, never contents: bodies and signatures are secrets-adjacent.
            _logger.LogInformation(
                "Webhook {Provider}: content type '{ContentType}', raw body {RawLength} chars, {FieldCount} form/query fields.",
                provider,
                Request.ContentType ?? "none",
                request.RawBody.Length,
                request.Form.Count);

            var acknowledgement = await _paymentService.HandleWebhookAsync(provider, request);

            return acknowledgement is null ? Ok() : Ok(acknowledgement);
        }

        [HttpPost("{id:int}/refund")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> RefundPayment(int id)
        {
            await _paymentService.RefundPaymentAsync(id, GetCurrentUserId());
            return Ok(new { message = "Payment refunded successfully." });
        }

        /// <summary>
        /// Collects everything a gateway might need, from every place a webhook can legitimately come from:
        /// the raw body (Stripe and WayForPay sign these exact bytes), form fields (LiqPay's real
        /// application/x-www-form-urlencoded callback) and the query string (manual testing).
        /// </summary>
        private async Task<GatewayRequest> ReadWebhookRequestAsync(CancellationToken cancellationToken)
        {
            // No-op when the global middleware already enabled buffering; makes this action
            // independent of it, so the body can be read here AND again by the form reader.
            Request.EnableBuffering();

            // 1. Raw body, always (an empty string when there is none).
            var rawBody = string.Empty;
            if (Request.Body.CanSeek)
            {
                Request.Body.Position = 0;
            }

            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync(cancellationToken);
            }

            if (Request.Body.CanSeek)
            {
                Request.Body.Position = 0;
            }

            var form = new Dictionary<string, string>(StringComparer.Ordinal);

            // 2. Real provider request (LiqPay): form fields.
            if (Request.HasFormContentType)
            {
                var formCollection = await Request.ReadFormAsync(cancellationToken);
                foreach (var field in formCollection)
                {
                    form[field.Key] = field.Value.ToString();
                }
            }

            // 3. Manual testing: ?data=...&signature=...  (never overrides a real form field)
            foreach (var queryField in Request.Query)
            {
                if (!form.ContainsKey(queryField.Key))
                {
                    form[queryField.Key] = queryField.Value.ToString();
                }
            }

            var headers = Request.Headers.ToDictionary(
                header => header.Key,
                header => header.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            return new GatewayRequest(rawBody, form, headers);
        }
    }
}
