using System.Diagnostics;
using AdminPanel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Error()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (feature?.Error != null)
            {
                _logger.LogError(feature.Error, "Unhandled exception at {Path}", feature.Path);
            }

            return View(new ErrorViewModel
            {
                StatusCode = 500,
                Title = "Something went wrong",
                Message = "The action could not be completed. The error has been written to the application log.",
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [Route("Home/StatusCode")]
        public IActionResult StatusCodeHandler(int? code)
        {
            var status = code ?? 500;

            var model = status switch
            {
                404 => new ErrorViewModel
                {
                    StatusCode = 404,
                    Title = "Page not found",
                    Message = "The page you asked for does not exist or has been moved."
                },
                403 => new ErrorViewModel
                {
                    StatusCode = 403,
                    Title = "Access denied",
                    Message = "Your role does not allow you to open this section."
                },
                _ => new ErrorViewModel
                {
                    StatusCode = status,
                    Title = "Request failed",
                    Message = $"The server returned status {status}."
                }
            };

            Response.StatusCode = status;
            return View("Error", model);
        }
    }
}
