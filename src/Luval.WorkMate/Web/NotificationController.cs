using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Text.Json;
using System.Text.Json.Serialization;
using Luval.WorkMate.Core.Services;

namespace Luval.WorkMate.Web
{

    [Route("api/notifications")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly EmailAttachmentService _emailAttachmentService;
        public NotificationController(EmailAttachmentService emailAttachmentService, ILoggerFactory loggerFactory)
        {
            if(loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger(GetType().Name);
            _emailAttachmentService = emailAttachmentService ?? throw new ArgumentNullException(nameof(emailAttachmentService));
        }

        [AllowAnonymous]
        [HttpPost("handler")]
        public async Task<IActionResult> ReceiveNotification([FromQuery] string? validationToken)
        {
            _logger.LogInformation("ReceiveNotification called");

            // If there is a validation token in the query string,
            // send it back in a 200 OK text/plain response
            if (!string.IsNullOrEmpty(validationToken))
            {
                return Ok(validationToken);
            }

            // Use the Graph client's serializer to deserialize the body
            using var bodyStream = new MemoryStream();
            await Request.Body.CopyToAsync(bodyStream);
            bodyStream.Seek(0, SeekOrigin.Begin);

            // Calling RegisterDefaultDeserializer here isn't strictly necessary since
            // we have a GraphServiceClient instance. In cases where you do not have a
            // GraphServiceClient, you need to register the JSON provider before trying
            // to deserialize.
            ApiClientBuilder.RegisterDefaultDeserializer<JsonParseNodeFactory>();
            var notifications = await KiotaJsonSerializer.DeserializeAsync<ChangeNotificationCollection>(bodyStream);

            if (notifications == null || notifications.Value == null)
            {
                return Accepted();
            }

            var content = JsonSerializer.Serialize(notifications.Value, new JsonSerializerOptions(){
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            });

            _logger.LogInformation($"Received {notifications.Value.Count} notifications.\nHere is the content\n{content}");

            // Perform a background task to process the notifications
            PerformBackgroundTask(content);

            // Process the notification (e.g., parse the request body and act on new emails)
            return Accepted();
        }

        [AllowAnonymous]
        [HttpGet("healthcheck")]
        public string HealthCheck()
        {
            return "OK";
        }

        private async void PerformBackgroundTask(string jsonContent)
        {
            var ids = _emailAttachmentService.GetEmailIds(jsonContent);
            foreach (var id in ids)
            {
                await _emailAttachmentService.ProcessEmailAttachmentAsync(id);
            }
        } 
    }
}
