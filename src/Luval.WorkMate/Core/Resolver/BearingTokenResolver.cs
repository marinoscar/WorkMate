using Luval.AuthMate.Core.Entities;
using Luval.AuthMate.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Resolver
{
    public class BearingTokenResolver
    {


        private readonly AppConnectionService _connectionService;
        private readonly IConfiguration _config;
        private readonly ILogger<BearingTokenResolver> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConnectionResolver"/> class.
        /// </summary>
        /// <param name="connectionService">The service to manage application connections.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any of the parameters are null.
        /// </exception>
        public BearingTokenResolver(AppConnectionService connectionService, IConfiguration configuration, ILogger<BearingTokenResolver> logger)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Asynchronously retrieves an application connection for the specified provider.
        /// </summary>
        /// <param name="providerName">The name of the provider for which to retrieve the connection.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the application connection
        /// for the specified provider, or null if the connection could not be resolved.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the providerName is null or empty.</exception>
        public async Task<AppConnection> GetConnectionAsync(string providerName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentNullException(nameof(providerName));
            }

            var section = _config.GetSection("AppConnections");
            var providerSection = section[providerName];
            if (providerSection == null)
            {
                _logger.LogError("The AppConnections section is not found in the configuration file for provider: {ProviderName}", providerName);
                return null;
            }

            var userSection = _config.GetSection("AppConnections:User");
            var user = userSection["User"];
            if (user == null)
            {
                _logger.LogError("The AppConnections User property is not found in the configuration file");
                return null;
            }

            return await _connectionService.GetConnectionAsync(providerName, user, cancellationToken);
        }

        /// <summary>
        /// Asynchronously retrieves an access token for the specified provider.
        /// </summary>
        /// <param name="providerName">The name of the provider for which to retrieve the token.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the access token
        /// for the specified provider.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the providerName is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the connection could not be resolved.</exception>
        public async Task<string> GetTokenAsync(string providerName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentNullException(nameof(providerName));
            }

            var connection = await GetConnectionAsync(providerName, cancellationToken);
            if (connection == null)
            {
                throw new InvalidOperationException($"Connection for provider '{providerName}' could not be resolved.");
            }

            return await _connectionService.ResolveAccessTokenAsync(connection, cancellationToken);
        }
    }
}
