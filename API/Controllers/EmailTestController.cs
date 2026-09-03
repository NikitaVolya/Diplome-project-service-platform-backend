using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("test-email")]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailTestController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Send()
        {
            await _emailService.SendAsync(
                "test@example.com",
                "Test email",
                "Hello from ASP.NET Core!");

            return Ok();
        }
    }
}
