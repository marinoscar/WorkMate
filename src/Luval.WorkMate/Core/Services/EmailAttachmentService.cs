using Luval.AuthMate.Core;
using Luval.GenAIBotMate.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Drives.Item.Items.Item.GetActivitiesByIntervalWithStartDateTimeWithEndDateTimeWithInterval;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Services
{
    public class EmailAttachmentService
    {
        private readonly ILogger<EmailAttachmentService> _logger;
        private readonly GenAIBotService _genAIBotService;
        private readonly EmailService _emailService;

        private async Task ProcessEmailAttachmentAsync(string emailId, CancellationToken cancellationToken = default)
        {
            var email = await _emailService.GetEmailAsync(emailId, cancellationToken).ConfigureAwait(false);
            if (email == null) return;
            if (string.IsNullOrEmpty(email.From.EmailAddress.Address)) return;
            _logger.LogInformation("Processing email {0}", email.From.ToJson());
            if (!email.From.EmailAddress.Address.ToLowerInvariant().Contains("remarkable.com")) return;
            if (email.HasAttachments != null && !email.HasAttachments.Value)
            {
                _logger.LogInformation("Email {0} has no attachments", email.Id);
                return;
            }
            var attachments = await _emailService.GetEmailAttachmentsAsync(emailId, cancellationToken).ConfigureAwait(false);
            var imageAttachments = attachments.Where(i => IsImageType(i));
            
            if(imageAttachments == null || !imageAttachments.Any())
            {
                _logger.LogInformation("Email {0} has no image attachments", email.Id);
                return;
            }

            var history = new ChatHistory(GetSystemMessage());
            var collection = new ChatMessageContentItemCollection();
            foreach (var attachment in imageAttachments)
            {
                var img = new ImageContent(attachment.ContentBytes, attachment.ContentType);
            }
            collection.Add(new Microsoft.SemanticKernel.TextContent(GetPrompt()));

            var settings = new OpenAIPromptExecutionSettings() { 
                ModelId = "gpt4-o",
                Temperature = 0
            };

            var response = await _genAIBotService.GetChatMessageAsync(history, settings, cancellationToken).ConfigureAwait(false);
        }

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

        private string GetPrompt()
        {
            return @"
Please extract the text from the image, also do it in markdown inside a codeblock if possible
Addionally, create a json code block, the json will be fed into a TODO application that has the ability create tasks,
the structure of the task has a Title and then it has the ability to have To-do items, make sure to group the tasks
in a way that the task has a main objective and if required add some very tactical todo list.

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

        public static bool IsImageType(FileAttachment file)
        {
            if (file == null) return false;
            if (string.IsNullOrEmpty(file.ContentType)) return false;
            return AllowedImageMimeTypes.Contains(file.ContentType);
        }


    }
}
