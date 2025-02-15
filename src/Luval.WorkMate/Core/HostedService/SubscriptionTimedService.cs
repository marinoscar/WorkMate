﻿using Luval.WorkMate.Core.Resolver;
using Luval.WorkMate.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.HostedService
{
    /// <summary>
    /// A hosted service that runs the <see cref="SubscriptionService"/> at specified intervals.
    /// </summary>
    public class SubscriptionTimedService(IServiceProvider serviceProvider) : TimedHostedService(serviceProvider)
    {
        private SubscriptionService _service = default!;
        private bool isFirstTime = true;

        /// <summary>
        /// Executes the subscription service asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            //if (isFirstTime)
            //{
            //    Logger.LogInformation("First time, waiting for the next execution");
            //    isFirstTime = false;
            //    return;
            //}
            if (_service == null)
                //_service = ServiceScope.ServiceProvider.GetRequiredService<SubscriptionService>();
                _service = new SubscriptionService(ServiceScope.ServiceProvider.GetRequiredService<IConfiguration>(),
                        new WebUserAuthenticationResolver(ServiceScope.ServiceProvider),
                        ServiceScope.ServiceProvider.GetRequiredService<ILoggerFactory>());

            Logger.LogDebug("Running the subscription service");
            try
            {
                await _service.RunServiceAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }
        }
    }
}
