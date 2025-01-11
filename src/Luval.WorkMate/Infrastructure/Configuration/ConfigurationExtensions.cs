using Luval.AuthMate.Core.Interfaces;
using Luval.AuthMate.Core.Resolver;
using Luval.AuthMate.Core.Services;
using Luval.WorkMate.Core.Agent;
using Luval.WorkMate.Core.HostedService;
using Luval.WorkMate.Core.PlugIn;
using Luval.WorkMate.Core.Resolver;
using Luval.WorkMate.Core.Services;
using Luval.WorkMate.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Concurrent;
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
            s.AddScoped<IAuthenticationProvider, WebUserAuthenticationResolver>();
            s.AddScoped<TodoService>();
            s.AddScoped<OneNoteService>();
            s.AddScoped<WorkMateAgent>();
            s.AddScoped<BotResolver>();
            s.AddScoped<TodoTaskPlugIn>();
            s.AddScoped<DatePlugIn>();  
            s.AddScoped<AppConnectionService>();
            s.AddScoped<AgentConnectionService>();
            s.AddScoped<EmailService>();
            s.AddScoped<SubscriptionService>();
            s.AddScoped<EmailAttachmentService>();
            
            s.RemoveAll<IUserResolver>();

            s.AddScoped<ServiceUserResolver>();
            s.AddScoped<WebUserResolver>();
            s.AddScoped<IUserResolver, CompositeUserResolver>();

            s.AddHostedService<SubscriptionTimedService>();
            s.AddHostedService<EmailAttachmentTimeService>();

            s.AddSingleton(new UniqueConcurrentQueue<string, ChangeNotification>());
            return s;
        }

        /// <summary>
        /// Extracts the "Id" from a resource string in the format "Users/{userId}/Messages/{Id}".
        /// </summary>
        /// <param name="resource">The resource string containing the identifier.</param>
        /// <returns>The extracted "Id" from the resource string.</returns>
        /// <exception cref="ArgumentException">Thrown if the input is null, empty, or not in the expected format.</exception>
        public static string GetResourceId(this string resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentException("The resource string cannot be null, empty, or whitespace.", nameof(resource));
            }

            try
            {
                const string pattern = "Users/";
                const string messagesSegment = "/Messages/";

                // Ensure the string contains the expected segments
                if (!resource.Contains(pattern) || !resource.Contains(messagesSegment))
                {
                    throw new ArgumentException("The resource string is not in the expected format.");
                }

                // Find the indices of the relevant parts
                int startIndex = resource.IndexOf(messagesSegment) + messagesSegment.Length;

                if (startIndex < messagesSegment.Length || startIndex >= resource.Length)
                {
                    throw new ArgumentException("The resource string does not contain a valid 'Id' segment.");
                }

                // Extract the Id
                string id = resource.Substring(startIndex);
                return id;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while extracting the resource Id.", ex);
            }
        }
    }
}
