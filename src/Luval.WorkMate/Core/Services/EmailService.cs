using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph.Me.Messages;
using Microsoft.Graph.Models;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Me.Messages.Item.Forward;
using Microsoft.Graph;

namespace Luval.WorkMate.Core.Services
{
    /// <summary>
    /// Service class for interacting with Microsoft Graph API to manage emails.
    /// </summary>
    public class EmailService : MicrosoftGraphServiceBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailService"/> class.
        /// </summary>
        /// <param name="authProvider">The authentication provider for Microsoft Graph.</param>
        /// <param name="loggerFactory">The logger factory to create loggers.</param>
        public EmailService(IAuthenticationProvider authProvider, ILoggerFactory loggerFactory) : base(authProvider, loggerFactory)
        {
        }

        /// <summary>
        /// Retrieves a specific email from the user's mailbox by message ID.
        /// </summary>
        /// <param name="messageId">The unique identifier of the email message.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Message"/> object.</returns>
        public async Task<IEnumerable<Message>> GetEmailAsync(string? selectExpression, string? filterExpression, int? topCount, CancellationToken cancellationToken = default)
        {
            if (topCount.HasValue && topCount.Value <= 0)
            {
                throw new ArgumentException("Top count must be greater than zero.", nameof(topCount));
            }

            try
            {
                Action<RequestConfiguration<MessagesRequestBuilder.MessagesRequestBuilderGetQueryParameters>> requestConfig = (r) =>
                {
                    if (!string.IsNullOrWhiteSpace(selectExpression))
                        r.QueryParameters.Select = selectExpression.Split(",");
                    if (!string.IsNullOrWhiteSpace(filterExpression))
                        r.QueryParameters.Filter = filterExpression;
                    if (topCount.HasValue)
                        r.QueryParameters.Top = topCount.Value;
                };

                var result = await GraphClient.Me.Messages.GetAsync(requestConfiguration: requestConfig, cancellationToken: cancellationToken).ConfigureAwait(false);
                return result != null ? result.Value ?? [] : [];
            }
            catch (ServiceException ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving emails.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An unexpected error occurred.");
                throw;
            }
        }

        /// <summary>
        /// Sends an email using the Microsoft Graph API.
        /// </summary>
        /// <param name="email">The email message to send.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendEmailAsync(SendMailPostRequestBody email, CancellationToken cancellationToken = default)
        {
            if (email == null)
            {
                throw new ArgumentNullException(nameof(email), "Email message cannot be null.");
            }

            try
            {
                await GraphClient.Me.SendMail.PostAsync(email, cancellationToken: cancellationToken);
            }
            catch (ServiceException ex)
            {
                Logger.LogError(ex, "An error occurred while sending the email.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An unexpected error occurred.");
                throw;
            }
        }

        /// <summary>
        /// Forwards an email using the Microsoft Graph API.
        /// </summary>
        /// <param name="emailId">The unique identifier of the email message to forward.</param>
        /// <param name="request">The request body containing the details of the forward action.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ForwardEmailAsync(string emailId, ForwardPostRequestBody request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(emailId))
            {
                throw new ArgumentException("Email ID cannot be null or empty.", nameof(emailId));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Request body cannot be null.");
            }

            try
            {
                await GraphClient.Me.Messages[emailId].Forward.PostAsync(request, cancellationToken: cancellationToken);
            }
            catch (ServiceException ex)
            {
                Logger.LogError(ex, "An error occurred while forwarding the email.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An unexpected error occurred.");
                throw;
            }
        }

        /// <summary>
        /// Retrieves file attachments from a specific email in the user's mailbox.
        /// </summary>
        /// <param name="emailId">The unique identifier of the email message.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="FileAttachment"/> objects.</returns>
        public async Task<IEnumerable<FileAttachment?>> GetEmailAttachmentsAsync(string emailId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(emailId))
            {
                throw new ArgumentException("Email ID cannot be null or empty.", nameof(emailId));
            }

            try
            {
                var attachments = await GraphClient.Me.Messages[emailId].Attachments.GetAsync(cancellationToken: cancellationToken);
                if (attachments == null || attachments.Value == null) return [];
                return attachments.Value.Where(a => a != null && a is FileAttachment).Select(a => a as FileAttachment).ToList();
            }
            catch (ServiceException ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving email attachments.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An unexpected error occurred.");
                throw;
            }
        }
    }
}
