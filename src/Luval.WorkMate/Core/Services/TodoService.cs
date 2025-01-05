using Luval.AuthMate.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph.Me.Todo.Lists.Item.Tasks;
using Luval.WorkMate.Infrastructure.Data;

namespace Luval.WorkMate.Core.Services
{
    public class TodoService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly IAuthenticationProvider _authProvider;
        private readonly ILogger<TodoService> _logger;
        private const string openFilter = "not (status eq 'completed')";
        private const string completedFilter = "status eq 'completed'";

        /// <summary>
        /// Initializes a new instance of the <see cref="TodoService"/> class.
        /// </summary>
        /// <param name="graphClient">The GraphServiceClient instance.</param>
        /// <param name="authProvider">The authentication provider.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public TodoService(GraphServiceClient graphClient, IAuthenticationProvider authProvider, ILogger<TodoService> logger)
        {
            _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
            _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TodoTaskList?> CreateTaskList(string name, CancellationToken cancellationToken = default)
        {
            var list = new TodoTaskList() { DisplayName = name };
            return await _graphClient.Me.Todo.Lists.PostAsync(list, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<TodoTaskList>> GetTaskLists(CancellationToken cancellationToken = default)
        {
            var result = await _graphClient.Me.Todo.Lists.GetAsync(cancellationToken: cancellationToken);
            if (result == null) throw new InvalidOperationException("The result of the request is null");
            return result.Value ??= new List<TodoTaskList>();
        }

        public async Task<TodoTask?> CreateTaskAsync(string listId, string title, string? notes, bool highImportance, DateTime? reminderOn, DateTime? dueDate, CancellationToken cancellationToken = default)
        {
            var task = new TodoTask()
            {
                Title = title,
                Importance = highImportance ? Importance.High : Importance.Normal,
                Body = new ItemBody()
                {
                    Content = notes,
                    ContentType = BodyType.Text
                }
            };
            if (reminderOn != null) task.ReminderDateTime = new DateTimeTimeZone() { DateTime = reminderOn.Value.ToString("o"), TimeZone = TimeZoneInfo.Local.Id };
            if (dueDate != null) task.DueDateTime = new DateTimeTimeZone() { DateTime = dueDate.Value.ToString("o"), TimeZone = TimeZoneInfo.Local.Id };
            return await CreateTaskAsync(listId, task, cancellationToken: cancellationToken);
        }

        public async Task<TodoTask?> CreateTaskAsync(string listId, TodoTask task, LinkedResource? linkedResource = null, IEnumerable<string>? checkListItems = null, CancellationToken cancellationToken = default)
        {
            if (linkedResource != null)
                task.LinkedResources = new List<LinkedResource>() { linkedResource };
            if (checkListItems != null)
                task.ChecklistItems = checkListItems.Select(i => new ChecklistItem() { DisplayName = i }).ToList();

            return await _graphClient.Me.Todo.Lists[listId].Tasks.PostAsync(task, cancellationToken: cancellationToken);
        }

        public Task<IEnumerable<TodoTask>> GetOpenTasksAsync(string listId, CancellationToken cancellationToken = default)
        {
            return GetTasksAsync(listId, openFilter, cancellationToken);
        }

        public Task<IEnumerable<TodoTask>> GetCompletedTasksAsync(string listId, CancellationToken cancellationToken = default)
        {
            return GetTasksAsync(listId, completedFilter, cancellationToken);
        }

        public async Task<IEnumerable<TodoTask>> GetTasksAsync(string listId, string? filterExpression = null, CancellationToken cancellationToken = default)
        {
            Action<RequestConfiguration<TasksRequestBuilder.TasksRequestBuilderGetQueryParameters>> filter = default;

            if (!string.IsNullOrWhiteSpace(filterExpression))
            {
                filter = (r) => r.QueryParameters.Filter = filterExpression;
            }

            var result = await _graphClient.Me.Todo.Lists[listId].Tasks.GetAsync(requestConfiguration: filter, cancellationToken: cancellationToken);

            if (result == null) throw new InvalidOperationException("The result of the request is null");

            return result.Value ??= new List<TodoTask>();
        }

        public async Task<IEnumerable<TodoSetDto>> GetAllTasks(string? filterExpression = null, CancellationToken cancellationToken = default)
        {
            var lists = await GetTaskLists(cancellationToken);
            var result = new List<TodoSetDto>();
            foreach (var list in lists)
            {
                var tasks = await GetTasksAsync(list.Id, filterExpression, cancellationToken);
                result.Add(new TodoSetDto() { ListId = list.Id, DisplanyName = list.DisplayName, Tasks = tasks.ToList() });
            }
            return result;
        }

        public async Task<IEnumerable<TodoSetDto>> GetAllOpenTasks(string? filterExpression = null, CancellationToken cancellationToken = default)
        {
            return await GetAllTasks(openFilter, cancellationToken);
        }

        public async Task<IEnumerable<TodoSetDto>> GetAllCompletedTasks(string? filterExpression = null, CancellationToken cancellationToken = default)
        {
            return await GetAllTasks(completedFilter, cancellationToken);
        }

        public async Task AddChecklistItemAsync(string listId, string taskId, string displayName, CancellationToken cancellationToken)
        {
            await _graphClient.Me.Todo.Lists[listId].Tasks[taskId].ChecklistItems.PostAsync(new ChecklistItem() { DisplayName = displayName }, cancellationToken: cancellationToken);
        }

        public async Task<LinkedResource?> AddLinkedResourseAsync(string listId, string taskId, LinkedResource linkedResource, CancellationToken cancellationToken)
        {
            return await _graphClient.Me.Todo.Lists[listId].Tasks[taskId].LinkedResources.PostAsync(linkedResource, cancellationToken: cancellationToken);
        }
    }
}
