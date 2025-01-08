using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Infrastructure.Data
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Text.Json;
    using Microsoft.Graph.Models;
    using Markdig;

    /// <summary>
    /// Represents a response containing tasks parsed from an AI chat output.
    /// </summary>
    public class AITaskResponse
    {
        private readonly MarkdownPipeline _pipeline;

        /// <summary>
        /// List of tasks parsed from the AI chat output.
        /// </summary>
        public List<AIResponseTodoTaskDto> Tasks { get; set; } = [];

        /// <summary>
        /// The raw Markdown text extracted from the AI chat output.
        /// </summary>
        public string TaskText { get; set; } = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="AITaskResponse"/> class.
        /// </summary>
        public AITaskResponse()
        {
            _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions()
               .UsePipeTables()
               .UseMathematics()
               .UseDiagrams()
               .UseFigures()
               .UseAutoLinks()
               .UseBootstrap()
               .Build();
        }

        /// <summary>
        /// Parses the output of an AI chat and populates the TaskText and Tasks properties.
        /// The input must contain two code blocks: 
        /// the first for Markdown and the second for JSON.
        /// </summary>
        /// <param name="aiOutput">The AI chat output as a string.</param>
        /// <exception cref="ArgumentException">Thrown when the input string is null, empty, or improperly formatted.</exception>
        /// <exception cref="JsonException">Thrown when the JSON parsing fails.</exception>
        public void ParseAI(string aiOutput)
        {
            if (string.IsNullOrWhiteSpace(aiOutput))
            {
                throw new ArgumentException("The AI output cannot be null, empty, or whitespace.");
            }

            try
            {
                // Regex to match code blocks in the AI output
                var codeBlockRegex = new Regex(@"```(.*?)```", RegexOptions.Singleline);
                var matches = codeBlockRegex.Matches(aiOutput);

                // Ensure there are at least two code blocks
                if (matches.Count < 2)
                {
                    throw new ArgumentException("The AI output must contain at least two code blocks: one for Markdown and one for JSON.");
                }

                // First code block is Markdown (TaskText)
                TaskText = matches[0].Value.Replace("```markdown", "").Replace("```", "").Trim();

                // Second code block is JSON (Tasks)
                var jsonText = matches[1].Value.Replace("```json", "").Replace("```", "").Trim();

                Tasks = JsonSerializer.Deserialize<List<AIResponseTodoTaskDto>>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (Tasks == null)
                {
                    throw new JsonException("Failed to parse the JSON into a list of TodoTask objects.");
                }
            }
            catch (Exception ex)
            {
                // Log the error or handle as needed
                throw new Exception("An error occurred while parsing the AI output.", ex);
            }
        }

        /// <summary>
        /// Converts the list of AIResponseTodoTaskDto tasks to a list of TodoTaskRecord.
        /// </summary>
        /// <returns>A list of TodoTaskRecord objects.</returns>
        public List<TodoTaskRecord> GetTodoTaskRecords()
        {
            var records = new List<TodoTaskRecord>();
            foreach (var task in Tasks)
            {
                var body = new ItemBody
                {
                    Content = Markdown.ToHtml(task.Notes ?? "", _pipeline),
                    ContentType = BodyType.Html,
                };
                var todoTask = new TodoTask
                {
                    Title = task.Title,
                    Body = body,
                    ChecklistItems = task.ActionItems?.Select(a => new ChecklistItem { DisplayName = a }).ToList(),
                };
                if (task.DueDate.HasValue)
                {
                    todoTask.DueDateTime = new DateTimeTimeZone
                    {
                        DateTime = task.DueDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                        TimeZone = "Central Standard Time",
                    };
                }
                if (task.ReminderDate.HasValue)
                {
                    todoTask.ReminderDateTime = new DateTimeTimeZone
                    {
                        DateTime = task.ReminderDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                        TimeZone = "Central Standard Time",
                    };
                }
                records.Add(new TodoTaskRecord(todoTask, task.Category ?? ""));
            }
            return records;
        }
    }



    /// <summary>
    /// Represents a record that contains a TodoTask and its associated category.
    /// </summary>
    /// <param name="Task">The TodoTask object.</param>
    /// <param name="Category">The category of the task.</param>
    public record TodoTaskRecord(TodoTask Task, string Category);

    /// <summary>
    /// Represents a task with detailed information.
    /// </summary>
    public class AIResponseTodoTaskDto
    {

        public string? Category { get; set; }

        /// <summary>
        /// The title of the task.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Additional notes about the task.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// The due date of the task, if any.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// The reminder date of the task, if any.
        /// </summary>
        public DateTime? ReminderDate { get; set; }

        /// <summary>
        /// List of action items for the task.
        /// </summary>
        public List<string>? ActionItems { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AIResponseTodoTaskDto"/> class.
        /// </summary>
        public AIResponseTodoTaskDto()
        {
            ActionItems = new List<string>();
        }
    }

}
