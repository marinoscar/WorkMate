using Luval.AuthMate.Core.Entities;
using Luval.AuthMate.Core.Interfaces;
using Luval.AuthMate.Core.Services;
using Luval.AuthMate.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Services
{
    /// <summary>
    /// Service to manage agent connections.
    /// </summary>
    public class AgentConnectionService
    {
        private readonly IUserResolver _userResolver;
        private readonly AppConnectionService _appConnectionService;
        private readonly OAuthConnectionManager _oauthConnectionManager;
        private readonly ILogger<AgentConnectionService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentConnectionService"/> class.
        /// </summary>
        /// <param name="userResolver">The user resolver.</param>
        /// <param name="appConnectionService">The app connection service.</param>
        /// <param name="oauthConnectionManager">The OAuth connection manager.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public AgentConnectionService(IUserResolver userResolver, AppConnectionService appConnectionService, OAuthConnectionManager oauthConnectionManager, ILogger<AgentConnectionService> logger)
        {
            _userResolver = userResolver ?? throw new ArgumentNullException(nameof(userResolver));
            _appConnectionService = appConnectionService ?? throw new ArgumentNullException(nameof(appConnectionService));
            _oauthConnectionManager = oauthConnectionManager ?? throw new ArgumentNullException(nameof(oauthConnectionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sets up the connection asynchronously.
        /// </summary>
        /// <param name="refreshRequired">Action to be taken when a refresh is required.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The established AppConnection or null if a refresh is required.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the refreshRequired parameter is null.</exception>
        /// <exception cref="Exception">Thrown when an error occurs while setting up the connection.</exception>
        public async Task<AppConnection?> SetupConnectionAsync(Action<OAuthConnectionConfig, AppUser> refreshRequired, CancellationToken cancellationToken = default)
        {
            if (refreshRequired == null) throw new ArgumentNullException(nameof(refreshRequired));

            try
            {
                var user = _userResolver.GetUser();
                var config = _oauthConnectionManager.GetConfiguration("Microsoft");
                var connection = await _appConnectionService.GetConnectionAsync("Microsoft", _userResolver.GetUserEmail(), cancellationToken);

                if (connection == null) // need to create one
                {
                    _logger.LogInformation("Connection not found. Refresh required.");
                    refreshRequired(config, user);
                    return null;
                }

                if (connection.HasExpired && string.IsNullOrEmpty(connection.RefreshToken))
                {
                    _logger.LogInformation("Connection has expired and no refresh token is available. Refresh required.");
                    refreshRequired(config, user);
                    return null;
                }

                if (connection.HasExpired)
                {
                    _logger.LogInformation("Connection has expired. Refreshing token.");
                    connection = await _appConnectionService.RefreshTokenAsync(config, connection, cancellationToken);
                }

                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while setting up the connection.");
                throw;
            }
        }
    }
}
