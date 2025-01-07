using Luval.WorkMate.Infrastructure.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Services
{
    public class SubscriptionService : MicrosoftGraphServiceBase
    {
        private readonly IConfiguration _configuration;
        private readonly SubscriptionConfiguration _subscriptionConfiguration;
        private static bool _subscriptionsRetrieved = false;

        public SubscriptionService(IConfiguration configuration, IAuthenticationProvider authProvider, ILoggerFactory loggerFactory) : base(authProvider, loggerFactory)
        {
            _configuration = configuration ?? throw new ArgumentException(nameof(configuration));
            _subscriptionConfiguration = new SubscriptionConfiguration(_configuration);
        }

        public static Dictionary<string, Subscription> Subscriptions { get; private set; } = [];

        public async Task CreateSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            //first loads all into memory if it is the first time
            if (!_subscriptionsRetrieved) 
                await LoadAllAsync(cancellationToken);

            foreach (var sub in _subscriptionConfiguration.Subscriptions)
            {
                var subscription = new Subscription
                {
                    ChangeType = sub.ChangeType,
                    NotificationUrl = GetNotificationUrl(sub),
                    Resource = sub.Resource,
                    ExpirationDateTime = DateTimeOffset.UtcNow.AddMinutes(sub.DurationInMinutes),
                    ClientState = Guid.NewGuid().ToString()
                };
                await CreateSubscriptionAsync(subscription, cancellationToken);
            }
        }

        public async Task<Subscription?> CreateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default)
        {
            if(subscription == null) throw new ArgumentNullException(nameof(subscription));
            try
            {
                subscription = await GraphClient.Subscriptions.PostAsync(subscription, cancellationToken: cancellationToken);

                if (subscription != null && !string.IsNullOrEmpty(subscription.Id))
                    Subscriptions[subscription.Id] = subscription;

                Logger.LogInformation($"Subscription {subscription.ChangeType} for {subscription.Resource}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error {subscription.ChangeType} subscription for {subscription.Resource}");
            }
            return subscription;
        }

        public async Task RenewSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            var expWindow = DateTimeOffset.UtcNow.AddMinutes(_subscriptionConfiguration.RenewalWindowInMinutes);
            var subs = Subscriptions.Values.Where(s => s.ExpirationDateTime > expWindow);
            Logger.LogInformation($"Renewing {subs.Count()} subscriptions");
            foreach (var sub in subs)
            {
                sub.ExpirationDateTime = DateTimeOffset.UtcNow.AddMinutes(_subscriptionConfiguration.Subscriptions.First(s => s.Resource == sub.Resource).DurationInMinutes);
                await GraphClient.Subscriptions[sub.Id].PatchAsync(sub, cancellationToken: cancellationToken);
                Logger.LogInformation($"Renewed subscription {sub.Id} resource {sub.Resource}");
            }
        }

        public async Task LoadAllAsync(CancellationToken cancellationToken)
        {
            if (!_subscriptionsRetrieved)
            {
                var subs = await GraphClient.Subscriptions.GetAsync();
                _subscriptionsRetrieved = true;
                if (subs == null || subs.Value == null) return;
                foreach (var sub in subs.Value)
                {
                    if (!string.IsNullOrEmpty(sub.Id))
                        Subscriptions[sub.Id] = sub;
                }
                _subscriptionsRetrieved = true;
            }
        }

        private bool IsDev()
        {
            var environmentName = _configuration["ASPNETCORE_ENVIRONMENT"];
            if (environmentName == "Development")
            {
                return true;
            }
            return false;
        }

        private string GetNotificationUrl(SubscriptionItem sub)
        {
            return IsDev() ? sub.DevNotificationUrl : sub.ProdNotificationUrl;
        }
    }
}
