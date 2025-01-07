using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Web
{

    [Route("api/notifications")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        public NotificationController(ILogger<NotificationController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ReceiveNotification()
        {
            _logger.LogInformation("ReceiveNotification called");

            // Handle subscription validation request
            if (Request.Headers.ContainsKey("validationToken"))
            {
                var validationToken = Request.Headers["validationToken"];
                _logger.LogInformation("Validation token received: {ValidationToken}", validationToken);
                return Content(validationToken, "text/plain");
            }

            // Handle notifications
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            _logger.LogInformation("Notification received: {RequestBody}", requestBody);

            // Process the notification (e.g., parse the request body and act on new emails)
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("healthcheck")]
        public string HealthCheck()
        {
            return "OK";
        }
    }
}
