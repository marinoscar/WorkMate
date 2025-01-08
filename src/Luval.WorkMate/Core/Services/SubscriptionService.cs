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
    /// <summary>
    /// Service for managing Microsoft Graph subscriptions.
    /// </summary>
    public class SubscriptionService : MicrosoftGraphServiceBase
    {
        private readonly IConfiguration _configuration;
        private readonly SubscriptionConfiguration _subscriptionConfiguration;
        private static bool _subscriptionsRetrieved = false;
        private static bool _intialized = false;

        /// <summary>
        /// Gets the list of subscriptions and keeps them in memory during the application lifecycle.
        /// </summary>
        public static Dictionary<string, Subscription> Subscriptions { get; private set; } = new Dictionary<string, Subscription>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionService"/> class.
        /// </summary>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="authProvider">The authentication provider for Microsoft Graph.</param>
        /// <param name="loggerFactory">The logger factory to create loggers.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="configuration"/> is null.</exception>
        public SubscriptionService(IConfiguration configuration, IAuthenticationProvider authProvider, ILoggerFactory loggerFactory) : base(authProvider, loggerFactory)
        {
            _configuration = configuration ?? throw new ArgumentException(nameof(configuration));
            _subscriptionConfiguration = new SubscriptionConfiguration(_configuration);
        }

        /// <summary>
        /// Runs the subscription service asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method initializes the subscription service if it has not been initialized yet by creating subscriptions.
        /// If the service is already initialized, it checks for subscription renewals and renews them if necessary.
        /// </remarks>
        public async Task RunServiceAsync(CancellationToken cancellationToken)
        {
            if (!_intialized || !Subscriptions.Any())
            {
                //create the subscriptions for the first time
                await CreateSubscriptionsAsync(cancellationToken);
                _intialized = true;
            }
            else
            {
                //check for renewals
                await RenewSubscriptionsAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Creates subscriptions asynchronously based on the configuration.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method first loads all existing subscriptions into memory if they have not been loaded yet.
        /// Then, it iterates through the subscription configuration and creates new subscriptions with the specified properties.
        /// </remarks>
        public async Task CreateSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            //first loads all into memory if it is the first time
            if (!_subscriptionsRetrieved)
                await LoadAllAsync(cancellationToken);

            foreach (var sub in _subscriptionConfiguration.Items)
            {
                if (Subscriptions.ContainsKey(sub.Resource))
                {
                    Logger.LogInformation($"Subscription for {sub.Resource} already exists. Expires on {Subscriptions[sub.Resource].ExpirationDateTime}");
                    continue;
                }
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

        /// <summary>
        /// Creates a new subscription asynchronously.
        /// </summary>
        /// <param name="subscription">The subscription to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The created subscription, or null if the creation failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the subscription is null.</exception>
        public async Task<Subscription?> CreateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default)
        {
            if (subscription == null) throw new ArgumentNullException(nameof(subscription));
            try
            {
                subscription = await GraphClient.Subscriptions.PostAsync(subscription, cancellationToken: cancellationToken);

                if (subscription != null && !string.IsNullOrEmpty(subscription.Id))
                    Subscriptions[subscription.Resource] = subscription;

                Logger.LogInformation($"Subscription {subscription.ChangeType} for {subscription.Resource} with Id: {subscription.Id} Expires on: {subscription.ExpirationDateTime}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error {subscription.ChangeType} subscription for {subscription.Resource}");
            }
            return subscription;
        }

        /// <summary>
        /// Renews subscriptions asynchronously based on the configuration.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RenewSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            var expWindow = DateTimeOffset.UtcNow.AddMinutes(_subscriptionConfiguration.RenewalWindowInMinutes);
            var subs = Subscriptions.Values.Where(s => s.ExpirationDateTime > expWindow);
            Logger.LogInformation($"Renewing {subs.Count()} subscriptions");
            foreach (var sub in subs)
            {
                sub.ExpirationDateTime = DateTimeOffset.UtcNow.AddMinutes(_subscriptionConfiguration.Items.First(s => s.Resource == sub.Resource).DurationInMinutes);
                await GraphClient.Subscriptions[sub.Id].PatchAsync(sub, cancellationToken: cancellationToken);
                Logger.LogInformation($"Renewed subscription {sub.Id} resource {sub.Resource}");
            }
        }

        /// <summary>
        /// Loads all existing subscriptions into memory asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
                        Subscriptions[sub.Resource] = sub;
                }
                var allSubs = string.Join("\n", Subscriptions.Select(s => s.Key));
                Logger.LogInformation($"Loaded subscriptions for resources \n {allSubs}");
                _subscriptionsRetrieved = true;
            }
        }

        /// <summary>
        /// Determines if the current environment is development.
        /// </summary>
        /// <returns>True if the environment is development; otherwise, false.</returns>
        private bool IsDev()
        {
            var environmentName = _configuration["ASPNETCORE_ENVIRONMENT"];
            if (environmentName == "Development")
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the notification URL based on the environment.
        /// </summary>
        /// <param name="sub">The subscription item.</param>
        /// <returns>The notification URL.</returns>
        private string GetNotificationUrl(SubscriptionItem sub)
        {
            return IsDev() ? sub.DevNotificationUrl : sub.ProdNotificationUrl;
        }
    }
}
