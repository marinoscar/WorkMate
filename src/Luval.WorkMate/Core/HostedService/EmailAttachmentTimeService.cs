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
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.HostedService
{
    public class EmailAttachmentTimeService : TimedHostedService
    {
        private readonly UniqueConcurrentQueue<string, ChangeNotification> _queue;

        public EmailAttachmentTimeService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _queue = serviceProvider.GetRequiredService<UniqueConcurrentQueue<string, ChangeNotification>>();
        }
        public override async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug($"Total items in queue {_queue.Count}");
            var taskList = new List<Task>()
            {
                RunProcessAsync(ServiceProvider, 1, cancellationToken),
                RunProcessAsync(ServiceProvider, 2, cancellationToken),
                RunProcessAsync(ServiceProvider, 3, cancellationToken),
                RunProcessAsync(ServiceProvider, 4, cancellationToken),
                RunProcessAsync(ServiceProvider, 5, cancellationToken),
            };

            await Task.WhenAll(taskList);
        }

        public async Task RunProcessAsync(IServiceProvider serviceProvider, int threadNo, CancellationToken cancellationToken)
        {
            if (!_queue.TryDequeue(out var changeNotification)) return;
            if(changeNotification == null || string.IsNullOrEmpty(changeNotification.Resource)) return;

            using (var scope = serviceProvider.CreateScope())
            {
                var service = new EmailAttachmentService(scope.ServiceProvider.GetRequiredService<ILogger<EmailAttachmentService>>(),
                            scope.ServiceProvider.GetRequiredService<GenAIBotService>(),
                            scope.ServiceProvider.GetRequiredService<EmailService>(),
                            scope.ServiceProvider.GetRequiredService<TodoService>(),
                            scope.ServiceProvider.GetRequiredService<OneNoteService>());

                var id = changeNotification.Resource.GetResourceId();

                Logger.LogInformation($"Thread: {threadNo} Processing email attachment with Id: {id}");

                await service.ProcessEmailAttachmentAsync(id, cancellationToken);
            }
        }

    }
}
