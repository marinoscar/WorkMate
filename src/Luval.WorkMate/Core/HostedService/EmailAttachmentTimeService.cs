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
    /// <summary>
    /// A hosted service that processes email attachments at regular intervals.
    /// </summary>
    public class EmailAttachmentTimeService : TimedHostedService
    {
        private readonly UniqueConcurrentQueue<string, ChangeNotification> _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAttachmentTimeService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance.</param>
        public EmailAttachmentTimeService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _queue = serviceProvider.GetRequiredService<UniqueConcurrentQueue<string, ChangeNotification>>();
        }

        /// <summary>
        /// Executes the work to be done at each interval.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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

        /// <summary>
        /// Processes a single email attachment.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance.</param>
        /// <param name="threadNo">The thread number for logging purposes.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RunProcessAsync(IServiceProvider serviceProvider, int threadNo, CancellationToken cancellationToken)
        {
            Logger.LogDebug($"Starting Thread: {threadNo}");
            if (!_queue.TryDequeue(out var changeNotification))
            {
                Logger.LogDebug($"Thread: {threadNo} no item in queue");
                return;
            }
            if (changeNotification == null || string.IsNullOrEmpty(changeNotification.Resource))
            {
                Logger.LogDebug($"Thread: {threadNo} item in queue is empty");
                return;
            }

            using (var scope = serviceProvider.CreateScope())
            {
                var service = new EmailAttachmentService(scope.ServiceProvider.GetRequiredService<ILogger<EmailAttachmentService>>(),
                            scope.ServiceProvider.GetRequiredService<GenAIBotService>(),
                            scope.ServiceProvider.GetRequiredService<EmailService>(),
                            scope.ServiceProvider.GetRequiredService<TodoService>(),
                            scope.ServiceProvider.GetRequiredService<OneNoteService>());

                var id = changeNotification.Resource.GetResourceId();

                Logger.LogDebug($"Thread: {threadNo} Processing email attachment with Id: {id}");

                await service.ProcessEmailAttachmentAsync(id, cancellationToken);
            }
        }
    }
}
