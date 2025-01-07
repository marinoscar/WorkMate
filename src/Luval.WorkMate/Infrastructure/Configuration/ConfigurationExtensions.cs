using Luval.AuthMate.Core.Services;
using Luval.WorkMate.Core.Agent;
using Luval.WorkMate.Core.HostedService;
using Luval.WorkMate.Core.PlugIn;
using Luval.WorkMate.Core.Resolver;
using Luval.WorkMate.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Infrastructure.Configuration
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddWorkMateServices(this IServiceCollection s)
        {
            s.AddScoped<BearingTokenResolver>();
            s.AddScoped<IAuthenticationProvider, AuthenticationResolver>();
            s.AddScoped<TodoService>();
            s.AddScoped<WorkMateAgent>();
            s.AddScoped<BotResolver>();
            s.AddScoped<TodoTaskPlugIn>();
            s.AddScoped<DatePlugIn>();  
            s.AddScoped<AppConnectionService>();
            s.AddScoped<AgentConnectionService>();
            s.AddScoped<EmailService>();
            s.AddSingleton<SubscriptionService>();

            s.AddHostedService<SubscriptionTimedService>();
            return s;
        }
    }
}
