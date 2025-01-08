using Luval.AuthMate.Core.Entities;
using Luval.AuthMate.Core.Interfaces;
using Luval.AuthMate.Core.Resolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Resolver
{
    /// <summary>
    /// Resolves the user information by combining the results from WebUserResolver and ServiceUserResolver.
    /// </summary>
    public class CompositeUserResolver : IUserResolver
    {
        private readonly WebUserResolver _webUserResolver;
        private readonly ServiceUserResolver _serviceUserResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeUserResolver"/> class.
        /// </summary>
        /// <param name="webUserResolver">The web user resolver.</param>
        /// <param name="serviceUserResolver">The service user resolver.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="webUserResolver"/> or <paramref name="serviceUserResolver"/> is null.
        /// </exception>
        public CompositeUserResolver(WebUserResolver webUserResolver, ServiceUserResolver serviceUserResolver)
        {
            _serviceUserResolver = serviceUserResolver ?? throw new ArgumentNullException(nameof(serviceUserResolver));
            _webUserResolver = webUserResolver ?? throw new ArgumentNullException(nameof(webUserResolver));
        }

        /// <summary>
        /// Converts the specified UTC date and time to the current user's local date and time.
        /// </summary>
        /// <param name="dateTime">The UTC date and time to convert.</param>
        /// <returns>The converted date and time in the user's local timezone.</returns>
        /// <exception cref="NotImplementedException">Thrown when the method is not implemented.</exception>
        public DateTime ConvertToUserDateTime(DateTime dateTime)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the current user as an <see cref="AppUser"/> object.
        /// </summary>
        /// <returns>The current user as an <see cref="AppUser"/> object.</returns>
        public AppUser GetUser()
        {
            return _webUserResolver.GetUser();
        }

        /// <summary>
        /// Gets the email of the current user.
        /// </summary>
        /// <returns>The email of the current user, or the service user email if the web user email is "Anonymous".</returns>
        public string GetUserEmail()
        {
            var email = _webUserResolver.GetUserEmail();
            if (email == "Anonymous") return _serviceUserResolver.GetUserEmail();
            return email;
        }

        /// <summary>
        /// Gets the username of the current user.
        /// </summary>
        /// <returns>The username of the current user, or the service user name if the web user name is "Anonymous".</returns>
        public string GetUserName()
        {
            var userName = _webUserResolver.GetUserName();
            if (userName == "Anonymous") return _serviceUserResolver.GetUserName();
            return userName;
        }

        /// <summary>
        /// Gets the timezone of the current user.
        /// </summary>
        /// <returns>The timezone of the current user as a <see cref="TimeZoneInfo"/> object.</returns>
        public TimeZoneInfo GetUserTimezone()
        {
            return _webUserResolver.GetUserTimezone();
        }
    }
}
