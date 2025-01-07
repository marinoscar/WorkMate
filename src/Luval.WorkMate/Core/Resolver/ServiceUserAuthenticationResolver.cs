using Luval.AuthMate.Core.Entities;
using Luval.AuthMate.Core.Interfaces;
using Luval.AuthMate.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Resolver
{
    /// <summary>
    /// Provides authentication for HTTP requests using bearer tokens for service users.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="ServiceProvider"/> instance</param>
    public class ServiceUserAuthenticationResolver(IServiceProvider serviceProvider) : AuthenticationResolverBase(serviceProvider)
    {
        protected override BearingTokenResolver GetTokenResolver(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new BearingTokenResolver(
                serviceProvider.GetRequiredService<AppConnectionService>(),
                serviceProvider.GetRequiredService<IConfiguration>(),
                new MyUserResolver(),
                logger.CreateLogger<BearingTokenResolver>()
            );
        }

        public class MyUserResolver : IUserResolver
        {
            public DateTime ConvertToUserDateTime(DateTime dateTime)
            {
                throw new NotImplementedException();
            }

            public AppUser GetUser()
            {
                return new AppUser()
                { 
                    Id = 1,
                    AccountId = 1,
                    Email = GetUserEmail(),
                    DisplayName = GetUserName(),
                    Timezone = GetUserTimezone().Id,
                    ProviderKey = Guid.NewGuid().ToString(),
                    ProfilePictureUrl = "ServiceUser",
                    ProviderType = "ServiceUser",
                    Version = 1
                };
            }

            public string GetUserEmail()
            {
                return "oscar.marin.saenz@gmail.com";
            }

            public string GetUserName()
            {
                return "Oscar Marin";
            }

            public TimeZoneInfo GetUserTimezone()
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            }
        }
    }
}
