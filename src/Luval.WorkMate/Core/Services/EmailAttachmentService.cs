using Luval.AuthMate.Core;
using Luval.GenAIBotMate.Core.Services;
using Luval.WorkMate.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;
using System.Text.Json;


namespace Luval.WorkMate.Core.Services
{
    /// <summary>  
    /// Service for processing email attachments, specifically images, and extracting tasks from them using AI.  
    /// </summary>  
    public class EmailAttachmentService
    {
        private readonly ILogger<EmailAttachmentService> _logger;
        private readonly GenAIBotService _genAIBotService;
        private readonly EmailService _emailService;
        private readonly TodoService _todoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAttachmentService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging information and errors.</param>
        /// <param name="genAIBotService">The AI bot service for processing chat messages.</param>
        /// <param name="emailService">The email service for retrieving and managing emails.</param>
        /// <param name="todoService">The to-do service for creating and managing tasks.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required dependencies are null.</exception>
        public EmailAttachmentService(
            ILogger<EmailAttachmentService> logger,
            GenAIBotService genAIBotService,
            EmailService emailService,
            TodoService todoService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _genAIBotService = genAIBotService ?? throw new ArgumentNullException(nameof(genAIBotService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
        }

        /// <summary>  
        /// Processes the email attachment asynchronously.  
        /// </summary>  
        /// <param name="emailId">The ID of the email to process.</param>  
        /// <param name="cancellationToken">A token to cancel the operation.</param>  
        public async Task ProcessEmailAttachmentAsync(string emailId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to process email attachment for email ID: {EmailId}", emailId);

            var email = await _emailService.GetEmailAsync(emailId, cancellationToken).ConfigureAwait(false);
            if (email == null)
            {
                _logger.LogWarning("Email with ID {EmailId} not found.", emailId);
                return;
            }

            if (string.IsNullOrEmpty(email.From.EmailAddress.Address))
            {
                _logger.LogWarning("Email from address is empty for email ID: {EmailId}", emailId);
                return;
            }

            _logger.LogInformation("Processing email from: {EmailFrom}", email.From.EmailAddress.Address);

            if (!email.From.EmailAddress.Address.ToLowerInvariant().Contains("remarkable.com"))
            {
                _logger.LogInformation("Email is not from a trusted domain: {EmailFrom}", email.From.EmailAddress.Address);
                return;
            }

            if (email.HasAttachments != null && !email.HasAttachments.Value)
            {
                _logger.LogInformation("Email {EmailId} has no attachments", email.Id);
                return;
            }

            var attachments = await _emailService.GetEmailAttachmentsAsync(emailId, cancellationToken).ConfigureAwait(false);
            var imageAttachments = attachments.Where(i => IsImageType(i));

            if (imageAttachments == null || !imageAttachments.Any())
            {
                _logger.LogInformation("Email {EmailId} has no image attachments", email.Id);
                return;
            }

            var history = new ChatHistory(GetSystemMessage());
            var collection = new ChatMessageContentItemCollection();
            foreach (var attachment in imageAttachments)
            {
                var img = new ImageContent(attachment.ContentBytes, attachment.ContentType);
                _logger.LogInformation("Processing image attachment: {AttachmentName}", attachment.Name);
            }
            collection.Add(new TextContent(GetPrompt()));

            var settings = new OpenAIPromptExecutionSettings()
            {
                ModelId = "gpt4-o",
                Temperature = 0
            };

            var response = await _genAIBotService.GetChatMessageAsync(history, settings, cancellationToken).ConfigureAwait(false);
            var text = new StringBuilder();
            foreach (var item in response.Items.OfType<TextContent>())
            {
                text.AppendLine(item.Text);
            }

            var taskResponse = new AITaskResponse();
            taskResponse.ParseAI(text.ToString());
            await CreateTodoTasksAsync(taskResponse, cancellationToken);

            _logger.LogInformation("Finished processing email attachment for email ID: {EmailId}", emailId);
        }

        /// <summary>
        /// Extracts email IDs from a JSON string containing a specific JSON structure.
        /// </summary>
        /// <param name="jsonContent">The JSON string containing the array of data.</param>
        /// <returns>A list of extracted email IDs.</returns>
        public List<string> GetEmailIds(string jsonContent)
        {
            _logger.LogInformation("Extracting email IDs from JSON content.");
            var jsonDocument = JsonDocument.Parse(jsonContent);
            return GetEmailIds(jsonDocument);
        }

        /// <summary>
        /// Extracts email IDs from a JsonDocument containing a specific JSON structure.
        /// </summary>
        /// <param name="jsonDocument">The JsonDocument containing the array of data.</param>
        /// <returns>A list of extracted email IDs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input JsonDocument is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the JSON structure is invalid or parsing fails.</exception>
        public List<string> GetEmailIds(JsonDocument jsonDocument)
        {
            if (jsonDocument == null)
            {
                _logger.LogError("The input JsonDocument cannot be null.");
                throw new ArgumentNullException(nameof(jsonDocument), "The input JsonDocument cannot be null.");
            }

            var emailIds = new List<string>();

            try
            {
                // Ensure the root is an array
                if (jsonDocument.RootElement.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogError("The JSON document must contain an array at the root.");
                    throw new InvalidOperationException("The JSON document must contain an array at the root.");
                }

                // Iterate through each element in the array
                foreach (var element in jsonDocument.RootElement.EnumerateArray())
                {
                    // Validate the presence of the "Resource" property
                    if (!element.TryGetProperty("Resource", out var resourceProperty) || resourceProperty.ValueKind != JsonValueKind.String)
                    {
                        _logger.LogError("Each JSON element must contain a valid 'Resource' property of type string.");
                        throw new InvalidOperationException("Each JSON element must contain a valid 'Resource' property of type string.");
                    }

                    // Extract the Resource property value
                    var resource = resourceProperty.GetString();

                    // Validate the format of the Resource string
                    if (string.IsNullOrWhiteSpace(resource) || !resource.Contains("/Messages/"))
                    {
                        _logger.LogError("Invalid 'Resource' value: {Resource}", resource);
                        throw new InvalidOperationException($"Invalid 'Resource' value: {resource}");
                    }

                    // Parse the ID from the Resource string
                    var parts = resource.Split('/');
                    if (parts.Length >= 4 && parts[2] == "Messages")
                    {
                        emailIds.Add(parts[3]);
                    }
                    else
                    {
                        _logger.LogError("The 'Resource' string is not in the expected format: {Resource}", resource);
                        throw new InvalidOperationException($"The 'Resource' string is not in the expected format: {resource}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while extracting email IDs.");
                throw new Exception("An error occurred while extracting email IDs.", ex);
            }

            return emailIds;
        }

        /// <summary>  
        /// Creates to-do tasks asynchronously based on the AI task response.  
        /// </summary>  
        /// <param name="taskResponse">The AI task response containing tasks to create.</param>  
        /// <param name="cancellationToken">A token to cancel the operation.</param>  
        private async Task CreateTodoTasksAsync(AITaskResponse taskResponse, CancellationToken cancellationToken = default)
        {
            var category = new Dictionary<string, string>();
            foreach (var item in taskResponse.GetTodoTaskRecords())
            {
                if (!category.ContainsKey(item.Category.Trim()))
                    category.Add(item.Category.Trim(), (await GetListAsync(item.Category.Trim(), cancellationToken)).Id);
                _logger.LogInformation("Creating task {0}", item.Task.Title);
                await _todoService.CreateTaskAsync(category[item.Category.Trim()], item.Task, cancellationToken);
            }
        }

        /// <summary>  
        /// Retrieves or creates a task list asynchronously based on the category.  
        /// </summary>  
        /// <param name="category">The category of the task list.</param>  
        /// <param name="cancellationToken">A token to cancel the operation.</param>  
        /// <returns>The task list.</returns>  
        private async Task<TodoTaskList> GetListAsync(string category, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(category)) category = "Tasks";
            var list = await _todoService.GetTaskLists($"displayName eq '{category.Trim()}'", cancellationToken).ConfigureAwait(false);
            if (list == null || !list.Any())
            {
                _logger.LogInformation("Creating list {0}", category);
                return await _todoService.CreateTaskListAsync(category, cancellationToken);
            }
            return list.First();
        }

        /// <summary>  
        /// Gets the system message for the AI prompt.  
        /// </summary>  
        /// <returns>The system message.</returns>  
        private string GetSystemMessage()
        {
            return @"  
        You are a highly intelligent and specialized assistant designed to analyze and process handwritten notes captured as images. Your primary task is to accurately extract the information contained in these notes and organize it into a structured JSON format. Your processing capabilities include:  

        - Text Extraction: Extract and transcribe all legible handwritten text from the images.  
        - Action Item Identification: Identify actionable tasks or items, such as tasks that start with verbs like ""Call,"" ""Submit,"" ""Send,"" or explicit mentions of to-dos.  
        - Date and Time Recognition: Detect and extract any dates, times, or temporal references mentioned in the notes.  
        - Topic Categorization: Identify the main topics or subjects mentioned in the notes.  
        - Summary Generation: Create a concise summary of the overall content.  
        - Contextual Parsing: Detect and annotate relevant metadata, such as bullet points, headers, or any unique formatting.  
        - Error Handling: If handwriting is illegible or information is ambiguous, provide a note indicating this.  
        ";
        }

        /// <summary>  
        /// Gets the prompt for the AI to process the image and extract tasks.  
        /// </summary>  
        /// <returns>The prompt.</returns>  
        private string GetPrompt()
        {
            return @"  
        Please extract the text from the image, also do it in markdown inside a codeblock if possible  
        Addionally, create a json code block, the json will be fed into a TODO application that has the ability create tasks,  
        the structure of the task has a Title and then it has the ability to have To-do items, make sure to group the tasks  
        in a way that the task has a main objective and if required add some very tactical todo list.  

        - Category of the task, use your judgement to determine if the task is Personal, Work, or if you don't know use Tasks as the category (key:category)  
        - Title (key:title)  
        - Description of the task (key:notes)  
        - Due date of the task if available (key:dueDate)  
        - Reminder date in case that the task need to be completed before the due date (key:reminderDate)  
        - List of action items, like calling someone, or very tactical short activities, it is just the name of the action item, needs to be an array of string (key:actionItems)  
        ";
        }

        private static readonly HashSet<string> AllowedImageMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
               {
                   "image/png",
                   "image/jpeg",
                   "image/jpg",
                   "image/gif",
                   "image/tiff",
                   "image/bmp"
               };

        /// <summary>  
        /// Determines if the file attachment is an image type.  
        /// </summary>  
        /// <param name="file">The file attachment to check.</param>  
        /// <returns>True if the file is an image type; otherwise, false.</returns>  
        public static bool IsImageType(FileAttachment file)
        {
            if (file == null) return false;
            if (string.IsNullOrEmpty(file.ContentType)) return false;
            return AllowedImageMimeTypes.Contains(file.ContentType);
        }
    }
}
