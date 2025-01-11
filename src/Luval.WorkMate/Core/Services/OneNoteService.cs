using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Graph.Me.Onenote.Sections;
using Microsoft.Graph.Me.Onenote.Sections.Item.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Net.Http.Headers;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Worksheets.Item.Charts.Item.Image;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luval.WorkMate.Core.Services
{
    /// <summary>
    /// Service class for interacting with OneNote in Microsoft Graph API.
    /// </summary>
    /// <param name="authProvider">The instance of <see cref="IAuthenticationProvider"/> for the graph api</param>
    /// <param name="loggerFactory">The logger factory to create loggers.</param>
    public class OneNoteService(IAuthenticationProvider authProvider, ILoggerFactory loggerFactory) : MicrosoftGraphServiceBase(authProvider, loggerFactory)
    {
        private const string imageBlockName = "imageBlock";
        private const string fileBlockName = "fileBlock";
        private string? _defaultNotbookId;

        /// <summary>
        /// Retrieves a list of OneNote sections.
        /// </summary>
        /// <param name="notebookId">The ID of the notebook.</param>
        /// <param name="filterExpression">The filter expression to apply to the sections query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of OneNote sections.</returns>
        public async Task<IEnumerable<OnenoteSection>> GetSectionsAsync(string notebookId, string? filterExpression, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("Retrieving OneNote sections with filter: {FilterExpression}", filterExpression);

                Action<RequestConfiguration<Microsoft.Graph.Me.Onenote.Notebooks.Item.Sections.SectionsRequestBuilder.SectionsRequestBuilderGetQueryParameters>>? config = default!;
                if (!string.IsNullOrEmpty(filterExpression))
                    config = (request) => request.QueryParameters.Filter = filterExpression;

                var sections = await GraphClient.Me.Onenote.Notebooks[notebookId].Sections.GetAsync(requestConfiguration: config, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (sections == null || sections.Value == null || !sections.Value.Any())
                {
                    Logger.LogWarning("No OneNote sections found.");
                    return Enumerable.Empty<OnenoteSection>();
                }

                Logger.LogInformation("Retrieved {Count} OneNote sections.", sections.Value.Count);
                return sections.Value;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving OneNote sections.");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a list of OneNote sections.
        /// </summary>
        /// <param name="filterExpression">The filter expression to apply to the sections query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of OneNote sections.</returns>
        public async Task<IEnumerable<OnenoteSection>> GetSectionsAsync(string? filterExpression, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_defaultNotbookId))
                await GetDefaultNotebookAsync(cancellationToken);
            return await GetSectionsAsync(_defaultNotbookId, filterExpression, cancellationToken);
        }


        ///<summary>
        /// Retrieves the default OneNote notebook.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The default OneNote notebook.</returns>
        public async Task<Notebook> GetDefaultNotebookAsync(CancellationToken cancellationToken = default)
        {
            var exp = "isDefault eq true";
            var notebooks = await GetNotebooksAsync(exp, cancellationToken);
            var defaultNotebook = notebooks.FirstOrDefault();
            _defaultNotbookId = defaultNotebook?.Id;
            return defaultNotebook;
        }

        ///<summary>
        /// Retrieves a list of OneNote notebooks.
        /// </summary>
        /// <param name="filterExpression">The filter expression to apply to the notebooks query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of OneNote notebooks.</returns>
        public async Task<IEnumerable<Notebook>> GetNotebooksAsync(string? filterExpression, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("Retrieving OneNote notebooks with filter: {FilterExpression}", filterExpression);

                Action<RequestConfiguration<Microsoft.Graph.Me.Onenote.Notebooks.NotebooksRequestBuilder.NotebooksRequestBuilderGetQueryParameters>>? config = default!;

                if (!string.IsNullOrEmpty(filterExpression))
                    config = (request) => request.QueryParameters.Filter = filterExpression;

                var notebooks = await GraphClient.Me.Onenote.Notebooks.GetAsync(requestConfiguration: config, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (notebooks == null || notebooks.Value == null || !notebooks.Value.Any())
                {
                    Logger.LogWarning("No OneNote notebooks found.");
                    return Enumerable.Empty<Notebook>();
                }

                Logger.LogInformation("Retrieved {Count} OneNote notebooks.", notebooks.Value.Count);
                return notebooks.Value;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving OneNote notebooks.");
                throw;
            }
        }

        /// <summary>
        /// Creates a new OneNote section.
        /// </summary>
        /// <param name="notebookId">The ID of the notebook.</param>
        /// <param name="sectionName">The name of the section to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created OneNote section.</returns>
        public async Task<OnenoteSection> CreateSectionAsync(string notebookId, string sectionName, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("Creating OneNote section with name: {SectionName}", sectionName);

                var section = await GraphClient.Me.Onenote.Notebooks[notebookId].Sections.PostAsync(new OnenoteSection
                {
                    DisplayName = sectionName
                }, cancellationToken: cancellationToken);

                Logger.LogInformation("Created OneNote section with ID: {SectionId}", section.Id);
                return section;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating OneNote section.");
                throw;
            }
        }

        /// <summary>
        /// Creates a new OneNote section.
        /// </summary>
        /// <param name="sectionName">The name of the section to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created OneNote section.</returns>
        public async Task<OnenoteSection> CreateSectionAsync(string sectionName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_defaultNotbookId))
                await GetDefaultNotebookAsync(cancellationToken);
            return await CreateSectionAsync(_defaultNotbookId, sectionName, cancellationToken);
        }

        /// <summary>
        /// Retrieves a list of OneNote pages in a specified section.
        /// </summary>
        /// <param name="notebookId">The ID of the notebook.</param>
        /// <param name="sectionId">The ID of the section.</param>
        /// <param name="filterExpression">The filter expression to apply to the pages query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of OneNote pages.</returns>
        public async Task<IEnumerable<OnenotePage>> GetPagesAsync(string notebookId, string sectionId, string? filterExpression, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("Retrieving OneNote pages for section ID: {SectionId} with filter: {FilterExpression}", sectionId, filterExpression);

                Action<RequestConfiguration<Microsoft.Graph.Me.Onenote.Notebooks.Item.Sections.Item.Pages.PagesRequestBuilder.PagesRequestBuilderGetQueryParameters>>? config = default!;
                if (!string.IsNullOrEmpty(filterExpression))
                    config = (request) => request.QueryParameters.Filter = filterExpression;

                var pages = await GraphClient.Me.Onenote.Notebooks[notebookId].Sections[sectionId].Pages.GetAsync(requestConfiguration: config, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (pages == null || pages.Value == null || !pages.Value.Any())
                {
                    Logger.LogWarning("No OneNote pages found for section ID: {SectionId}.", sectionId);
                    return Enumerable.Empty<OnenotePage>();
                }

                Logger.LogInformation("Retrieved {Count} OneNote pages for section ID: {SectionId}.", pages.Value.Count, sectionId);
                return pages.Value;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving OneNote pages for section ID: {SectionId}.", sectionId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a list of OneNote pages in a specified section.
        /// </summary>
        /// <param name="sectionId">The ID of the section.</param>
        /// <param name="filterExpression">The filter expression to apply to the pages query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of OneNote pages.</returns>
        public async Task<IEnumerable<OnenotePage>> GetPagesAsync(string sectionId, string? filterExpression, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_defaultNotbookId))
                await GetDefaultNotebookAsync(cancellationToken);
            return await GetPagesAsync(_defaultNotbookId, sectionId, filterExpression, cancellationToken);
        }

        /// <summary>
        /// Creates a new OneNote page in a specified section.
        /// </summary>
        /// <param name="notebookId">The ID of the notebook.</param>
        /// <param name="sectionId">The ID of the section.</param>
        /// <param name="title">The title of the page.</param>
        /// <param name="htmlContent">The HTML content of the page.</param>
        /// <param name="files">The list of files to attach to the page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created OneNote page.</returns>
        public async Task<OnenotePage?> CreatePageAsync(string notebookId, string sectionId, string title, string htmlContent, List<OnenoteFile>? files, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("Creating OneNote page in section ID: {SectionId} with title: {Title}", sectionId, title);

                var client = new HttpClient();
                var requestInfo = new RequestInformation()
                {
                    HttpMethod = Method.POST,
                    URI = new Uri("https://graph.microsoft.com/v1.0/")
                };

                var multipartContent = new MultipartFormDataContent();
                var contentPage = GetPageHtml(title, htmlContent, files);

                var page = new StringContent(contentPage, Encoding.UTF8, "text/html");
                multipartContent.Add(page, "Presentation");

                if (files != null && files.Any())
                {
                    foreach (var file in files)
                    {
                        var idx = files.IndexOf(file);
                        var img = new StreamContent(file.GetStream());
                        img.Headers.ContentType = file.GetMediaType();
                        multipartContent.Add(img, GetBlockName(file, idx));
                    }
                }

                await this.AuthProvider.AuthenticateRequestAsync(requestInfo, null, cancellationToken);

                var url = $"https://graph.microsoft.com/v1.0/me/onenote/sections/{sectionId}/pages";

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = multipartContent
                };

                request.Headers.Add("Authorization", requestInfo.Headers["Authorization"]);

                var response = await client.SendAsync(request, cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError($"Error creating OneNote page in Notebook {notebookId} on section {sectionId} with error: \n{responseContent}");
                    return null;
                }
                var pageResult = JsonSerializer.Deserialize<OnenotePage>(responseContent, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                });
                Logger.LogDebug($"Created OneNote Page\n{responseContent}");
                return pageResult;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating OneNote page in section ID: {SectionId}.", sectionId);
                throw;
            }
        }

        /// <summary>
        /// Creates a new OneNote page in a specified section.
        /// </summary>
        /// <param name="sectionId">The ID of the section.</param>
        /// <param name="title">The title of the page.</param>
        /// <param name="htmlContent">The HTML content of the page.</param>
        /// <param name="files">The list of files to attach to the page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created OneNote page.</returns>
        public async Task<OnenotePage> CreatePageAsync(string sectionId, string title, string htmlContent, List<OnenoteFile>? files, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_defaultNotbookId))
                await GetDefaultNotebookAsync(cancellationToken);
            return await CreatePageAsync(_defaultNotbookId, sectionId, title, htmlContent, files, cancellationToken);
        }

        private string GetPageHtml(string title, string content, List<OnenoteFile>? files)
        {
            var timeStamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz");
            var fileHtml = new StringBuilder();
            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    var index = files.IndexOf(file);
                    if (file.IsImage)
                        fileHtml.AppendLine($"<img src=\"name:{GetBlockName(file, index)}\" alt=\"an image on the page\" width=\"{file.Width}\" />");
                    else
                        fileHtml.AppendLine($"<object data-attachment=\"{file.Name}\" data=\"name:{GetBlockName(file, index)}\" type=\"{file.ContentType}\" />");
                }
            }
            return $@"
    <!DOCTYPE html>
    <html>
      <head>
        <title>{title}</title>
        <meta name='created' content='{timeStamp}' />
      </head>
      <body>
        {content}
        {fileHtml}
      </body>
    </html>
            ";
        }

        private string GetBlockName(OnenoteFile onenoteFile, int index)
        {
            return onenoteFile.IsImage ? $"{imageBlockName}{index}" : $"{fileBlockName}{index}";
        }

        /// <summary>
        /// Represents a file in OneNote.
        /// </summary>
        public class OnenoteFile
        {
            /// <summary>
            /// Gets or sets the content type of the file.
            /// </summary>
            public string ContentType { get; set; } = default!;

            /// <summary>
            /// Gets or sets the name of the file.
            /// </summary>
            public string Name { get; set; } = default!;

            /// <summary>
            /// Gets or sets the content of the file.
            /// </summary>
            public byte[] Content { get; set; } = default!;

            /// <summary>
            /// Indicates if the file is an image.
            /// </summary>
            public bool IsImage => ContentType.StartsWith("image/");

            public int Width { get; set; } = 300;

            /// <summary>
            /// Gets the media type of the file.
            /// </summary>
            /// <returns>The <see cref="MediaTypeHeaderValue"/>.</returns>
            public MediaTypeHeaderValue GetMediaType()
            {
                return new MediaTypeHeaderValue(ContentType);
            }

            /// <summary>
            /// Gets the content as a stream.
            /// </summary>
            /// <returns>A <see cref="Stream"/> with the content.</returns>
            public Stream GetStream()
            {
                return new MemoryStream(Content);
            }
        }
    }
}
