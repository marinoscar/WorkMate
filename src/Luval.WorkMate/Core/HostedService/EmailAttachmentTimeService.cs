using Luval.GenAIBotMate.Core.Services;
using Luval.WorkMate.Core.Services;
using Luval.WorkMate.Infrastructure.Configuration;
using Luval.WorkMate.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.HostedService
{
    public class EmailAttachmentTimeService : TimedHostedService
    {
        private readonly UniqueConcurrentQueue<string, ChangeNotification> _queue;
        private EmailAttachmentService _service;

        public EmailAttachmentTimeService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _queue = serviceProvider.GetRequiredService<UniqueConcurrentQueue<string, ChangeNotification>>();
        }
        public override async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            if (_service == null)
                _service = new EmailAttachmentService(ServiceScope.ServiceProvider.GetRequiredService<ILogger<EmailAttachmentService>>(),
                        ServiceScope.ServiceProvider.GetRequiredService<GenAIBotService>(),
                        ServiceScope.ServiceProvider.GetRequiredService<EmailService>(),
                        ServiceScope.ServiceProvider.GetRequiredService<TodoService>());

            Logger.LogDebug($"Total items in queue { _queue.Count }");

            if (!_queue.TryDequeue(out var changeNotification)) return;

            var id = changeNotification.Resource.GetResourceId();

            Logger.LogInformation($"Processing email attachment with Id: {id}");

            await _service.ProcessEmailAttachmentAsync(id, cancellationToken);
        }
    }
}
