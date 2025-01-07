using Microsoft.AspNetCore.Mvc;
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
        [HttpPost]
        public async Task<IActionResult> ReceiveNotification()
        {
            // Handle subscription validation request
            if (Request.Headers.ContainsKey("validationToken"))
            {
                var validationToken = Request.Headers["validationToken"];
                return Content(validationToken, "text/plain");
            }

            // Handle notifications
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            Console.WriteLine($"Notification received: {requestBody}");

            // Process the notification (e.g., parse the request body and act on new emails)
            return Ok();
        }
    }
}
