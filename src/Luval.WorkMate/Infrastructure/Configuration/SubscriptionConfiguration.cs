using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Infrastructure.Configuration
{
    /// <summary>
    /// Represents the configuration for subscriptions.
    /// </summary>
    public class SubscriptionConfiguration
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionConfiguration"/> class.
        /// </summary>
        /// <param name="configuration">The configuration instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the configuration is null.</exception>
        public SubscriptionConfiguration(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Items = default!;
            Initialize();
        }

        /// <summary>
        /// Gets the list of subscriptions.
        /// </summary>
        public List<SubscriptionItem> Items { get; private set; }

        /// <summary>
        /// Gets the the time in minutes to renew the subscription before it expires, default to 60 min befire expiration.
        /// </summary>
        public int RenewalWindowInMinutes { get; private set; } = 60;


        /// <summary>
        /// Initializes the subscription configuration by loading the subscriptions from the configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no subscriptions are found or the configuration section is not properly defined.</exception>
        public void Initialize()
        {
            var subSection = _configuration.GetSection("Subscriptions");
            var subSectionItems = _configuration.GetSection("Subscriptions:Items");

            if (!subSection.Exists()) throw new InvalidOperationException("No subscriptions found in configuration");
            if (!subSectionItems.Exists()) throw new InvalidOperationException("No subscription items found in configuration");

            var subs = subSectionItems.Get<List<SubscriptionItem>>();
            if (subs == null || !subs.Any()) throw new InvalidOperationException("Subscriptions configuration section is not properly defined");
            if (!string.IsNullOrEmpty(subSection["RenewalWindowInMinutes"]))
            {
                RenewalWindowInMinutes = int.TryParse(subSection["RenewalWindowInMinutes"], out var rw) ? rw : 60;
            }

            Items = subs;
        }
    }

    public class SubscriptionItem
    {
        public string ChangeType { get; set; } = "created,updated";
        public string DevNotificationUrl { get; set; } = default!;
        public string ProdNotificationUrl { get; set; } = default!;
        public string Resource { get; set; } = default!;
        public int DurationInMinutes { get; set; } = (5 * 60);
    }
}
