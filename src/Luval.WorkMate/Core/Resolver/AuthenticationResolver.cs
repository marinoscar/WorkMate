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
    public class AuthenticationResolver : IAuthenticationProvider
    {
        private readonly BearingTokenResolver _tokenResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationResolver"/> class.
        /// </summary>
        /// <param name="tokenResolver">The token resolver to use for retrieving tokens.</param>
        /// <exception cref="ArgumentNullException">Thrown when the tokenResolver is null.</exception>
        public AuthenticationResolver(BearingTokenResolver tokenResolver)
        {
            _tokenResolver = tokenResolver ?? throw new ArgumentNullException(nameof(tokenResolver));
        }

        /// <summary>
        /// Authenticates the specified HTTP request by adding a bearer token to its headers.
        /// </summary>
        /// <param name="request">The HTTP request to authenticate.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var token = await _tokenResolver.GetTokenAsync("Microsoft");
            request.Headers.Add("Authorization", $"Bearer {token}");
        }

        /// <summary>
        /// Authenticates the specified request information by adding a bearer token to its headers.
        /// </summary>
        /// <param name="request">The request information to authenticate.</param>
        /// <param name="additionalAuthenticationContext">Additional context for authentication.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Headers == null) throw new ArgumentNullException(nameof(request.Headers));
            var token = await _tokenResolver.GetTokenAsync("Microsoft");
            request.Headers.TryAdd("Authorization", $"Bearer {token}");
        }
    }
}
