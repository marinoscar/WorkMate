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
    /// Base class for resolving authentication by providing bearer tokens.
    /// </summary>
    public abstract class AuthenticationResolverBase : IAuthenticationProvider
    {
        private readonly BearingTokenResolver _tokenResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationResolverBase"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve dependencies.</param>
        /// <exception cref="InvalidOperationException">Thrown when the token resolver cannot be resolved.</exception>
        protected AuthenticationResolverBase(IServiceProvider serviceProvider)
        {
            _tokenResolver = GetTokenResolver(serviceProvider);
            if (_tokenResolver == null) throw new InvalidOperationException($"Unabled to resolve the BearingTokenResolver by invoking GetTokenResolver()");
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BearingTokenResolver"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to solve dependencies.</param>
        /// <returns>A new instance of <see cref="BearingTokenResolver"/>.</returns>
        protected abstract BearingTokenResolver GetTokenResolver(IServiceProvider serviceProvider);

        /// <summary>
        /// Authenticates the specified HTTP request by adding a bearer token to its headers.
        /// </summary>
        /// <param name="request">The HTTP request to authenticate.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        public virtual async Task AuthenticateRequestAsync(HttpRequestMessage request)
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
        /// <exception cref="ArgumentNullException">Thrown when the request or its headers are null.</exception>
        public virtual async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Headers == null) throw new ArgumentNullException(nameof(request.Headers));
            var token = await _tokenResolver.GetTokenAsync("Microsoft");
            request.Headers.TryAdd("Authorization", $"Bearer {token}");
        }
    }
}
