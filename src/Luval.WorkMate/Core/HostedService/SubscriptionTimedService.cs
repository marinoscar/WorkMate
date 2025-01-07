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
        public SubscriptionTimedService(SubscriptionService service, IConfiguration configuration, ILoggerFactory loggerFactory) : base(configuration, loggerFactory)
        {
        }

        public override Task DoWorkAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
