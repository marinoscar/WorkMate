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

namespace Luval.WorkMate.Core.Services
{
    public class OneNoteService(IAuthenticationProvider authProvider, ILoggerFactory loggerFactory) : MicrosoftGraphServiceBase(authProvider, loggerFactory)
    {

        private const string imageBlockName = "imageBlock";
        private const string fileBlockName = "fileBlock";

        public async Task<IEnumerable<OnenoteSection>> GetSectionsAsync(string? filterExpression, CancellationToken cancellationToken = default)
        {
            Action<RequestConfiguration<SectionsRequestBuilder.SectionsRequestBuilderGetQueryParameters>>? config = default!;
            if (!string.IsNullOrEmpty(filterExpression))
                config = (request) => request.QueryParameters.Filter = filterExpression;

            var sections = await GraphClient.Me.Onenote.Sections.GetAsync(requestConfiguration: config, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (sections == null || sections.Value == null || !sections.Value.Any())
                return [];

            return sections.Value;
        }

        public async Task<OnenoteSection> CreateSectionAsync(string sectionName, CancellationToken cancellationToken = default)
        {
            return await GraphClient.Me.Onenote.Sections.PostAsync(new OnenoteSection
            {
                DisplayName = sectionName
            }, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<OnenotePage>> GetPagesAsync(string sectionId, string? filterExpression, CancellationToken cancellationToken = default)
        {
            Action<RequestConfiguration<PagesRequestBuilder.PagesRequestBuilderGetQueryParameters>>? config = default!;
            if (!string.IsNullOrEmpty(filterExpression))
                config = (request) => request.QueryParameters.Filter = filterExpression;

            var pages = await GraphClient.Me.Onenote.Sections[sectionId].Pages.GetAsync(requestConfiguration: config, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (pages == null || pages.Value == null || !pages.Value.Any())
                return [];
            return pages.Value;
        }

        public async Task<OnenotePage> CreatePageAsync(string sectionId, string title, string htmlContent, List<OnenoteFile>? files, CancellationToken cancellationToken = default)
        {
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


            // create a request information instance and make a request.
            var requestInformation = GraphClient.Me.Onenote.Sections[sectionId].Pages.ToGetRequestInformation();
            requestInformation.Headers.Add("Content-Type", multipartContent.Headers.ContentType.ToString());
            requestInformation.HttpMethod = Method.POST;
            requestInformation.Content = await multipartContent.ReadAsStreamAsync();
            var errorMapping = new Dictionary<string, ParsableFactory<IParsable>> {
              {"4XX", ODataError.CreateFromDiscriminatorValue},
              {"5XX", ODataError.CreateFromDiscriminatorValue},
            };
            return await GraphClient.RequestAdapter.SendAsync<OnenotePage>(requestInformation, OnenotePage.CreateFromDiscriminatorValue, errorMapping);
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
        /// Gets or sets the content type of the file
        /// </summary>
        public string ContentType { get; set; } = default!;

        /// <summary>
        /// Gets or sets the name of the file
        /// </summary>
        public string Name { get; set; } = default!;
        /// <summary>
        /// Gets or sets the content of the file
        /// </summary>
        public byte[] Content { get; set; } = default!;
        /// <summary>
        /// Indicates if the file is an image
        /// </summary>
        public bool IsImage => ContentType.StartsWith("image/");

        public int Width { get; set; } = 300;

        /// <summary>
        /// Gets the media type of the file
        /// </summary>
        /// <returns>The <see cref="MediaTypeHeaderValue"/></returns>
        public MediaTypeHeaderValue GetMediaType()
        {
            return new MediaTypeHeaderValue(ContentType);
        }

        /// <summary>
        /// Gets the content as a stream
        /// </summary>
        /// <returns>A <see cref="Stream"/> with the content</returns>
        public Stream GetStream()
        {
            return new MemoryStream(Content);
        }

    }
}