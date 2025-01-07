using Luval.AuthMate.Core.Interfaces;
using Luval.AuthMate.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Resolver
{
    /// <summary>
    /// Provides authentication for HTTP requests using bearer tokens.
    /// </summary>
    public class WebUserAuthenticationResolver(IServiceProvider serviceProvider) : AuthenticationResolverBase(serviceProvider), IAuthenticationProvider
    {
        protected override BearingTokenResolver GetTokenResolver(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new BearingTokenResolver(
                serviceProvider.GetRequiredService<AppConnectionService>(),
                serviceProvider.GetRequiredService<IConfiguration>(),
                serviceProvider.GetRequiredService<IUserResolver>(),
                logger.CreateLogger<BearingTokenResolver>()
            );
        }
    }
}
