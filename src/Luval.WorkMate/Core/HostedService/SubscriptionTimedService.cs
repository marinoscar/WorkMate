using Luval.WorkMate.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.HostedService
{
    public class SubscriptionTimedService : TimedHostedService
    {
        private readonly SubscriptionService _service;

        public SubscriptionTimedService(SubscriptionService service, IConfiguration configuration, ILoggerFactory loggerFactory) : base(configuration, loggerFactory)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public override async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            await _service.RunServiceAsync(cancellationToken);
        }
    }
}
