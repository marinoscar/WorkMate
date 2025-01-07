using Microsoft.Extensions.Logging;
using Microsoft.Graph;
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
    /// Base service class for interacting with Microsoft Graph API.
    /// </summary>
    public abstract class MicrosoftGraphServiceBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftGraphServiceBase"/> class.
        /// </summary>
        /// <param name="authProvider">The authentication provider for Microsoft Graph.</param>
        /// <param name="loggerFactory">The logger factory to create loggers.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="authProvider"/> or <paramref name="loggerFactory"/> is null.</exception>
        protected MicrosoftGraphServiceBase(IAuthenticationProvider authProvider, ILoggerFactory loggerFactory)
        {
            AuthProvider = authProvider ?? throw new ArgumentException(nameof(authProvider));
            if (loggerFactory == null) throw new ArgumentException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(GetType().Name);
            GraphClient = new GraphServiceClient(authProvider);
        }

        /// <summary>
        /// Gets the GraphServiceClient instance used to interact with Microsoft Graph.
        /// </summary>
        protected virtual GraphServiceClient GraphClient { get; private set; }

        /// <summary>
        /// Gets the authentication provider used for Microsoft Graph.
        /// </summary>
        protected virtual IAuthenticationProvider AuthProvider { get; private set; }

        /// <summary>
        /// Gets the logger instance used for logging.
        /// </summary>
        protected virtual ILogger Logger { get; private set; }

        /// <summary>
        /// Tests the connection to Microsoft Graph by retrieving the user's profile.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the connection is successful; otherwise, false.</returns>
        /// <exception cref="Exception">Thrown when there is an error testing the connection.</exception>
        public virtual async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            User? profile = default;
            try
            {
                profile = await GraphClient.Me.GetAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error testing connection to Microsoft Graph");
                return false;
            }
            return profile != null;
        }
    }
}
