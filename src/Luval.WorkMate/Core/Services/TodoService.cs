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
using Microsoft.Graph.Me.Todo.Lists;
using Luval.WorkMate.Infrastructure.Data;

namespace Luval.WorkMate.Core.Services
{
    public class TodoService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly IAuthenticationProvider _authProvider;
        private readonly ILogger<TodoService> _logger;
        public const string OpenTaskFilter = "status ne 'completed'";
        public const string CompletedTaskFilter = "status eq 'completed'";

        /// <summary>
        /// Initializes a new instance of the <see cref="TodoService"/> class.
        /// </summary>
        /// <param name="authProvider">The authentication provider.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public TodoService(IAuthenticationProvider authProvider, ILogger<TodoService> logger)
        {
            _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _graphClient = new GraphServiceClient(_authProvider);
        }

        /// <summary>
        /// Tests the connection to Microsoft Graph by retrieving the user's profile.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the connection is successful; otherwise, false.</returns>
        /// <exception cref="Exception">Thrown when there is an error testing the connection.</exception>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            User profile = default;
            try
            {
                profile = await _graphClient.Me.GetAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection to Microsoft Graph");
                return false;
            }
            return profile != null;
        }

        /// <summary>
        /// Creates a new task list.
        /// </summary>
        /// <param name="name">The name of the task list.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created task list.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public async Task<TodoTaskList?> CreateTaskListAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Task list name cannot be null or empty.", nameof(name));

            var list = new TodoTaskList() { DisplayName = name };
            try
            {
                return await _graphClient.Me.Todo.Lists.PostAsync(list, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task list with name {Name}", name);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all task lists.
        /// </summary>
        /// <param name="filterExpression">The OData filter expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of task lists.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the result of the request is null.</exception>
        public async Task<IEnumerable<TodoTaskList>> GetTaskLists(string? filterExpression = null, CancellationToken cancellationToken = default)
        {
            try
            {
                Action<RequestConfiguration<ListsRequestBuilder.ListsRequestBuilderGetQueryParameters>> filter = default;

                if (!string.IsNullOrWhiteSpace(filterExpression))
                {
                    filter = (r) => r.QueryParameters.Filter = filterExpression;
                }

                var result = await _graphClient.Me.Todo.Lists.GetAsync(requestConfiguration: filter, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (result == null) throw new InvalidOperationException("The result of the request is null");
                return result.Value ??= new List<TodoTaskList>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task lists");
                throw;
            }
        }

        /// <summary>
        /// Creates a new task in the specified task list.
        /// </summary>
        /// <param name="listId">The ID of the task list.</param>
        /// <param name="title">The title of the task.</param>
        /// <param name="notes">The notes for the task.</param>
        /// <param name="highImportance">Indicates if the task is of high importance.</param>
        /// <param name="reminderOn">The reminder date and time.</param>
        /// <param name="dueDate">The due date and time.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created task.</returns>
        /// <exception cref="ArgumentException">Thrown when the listId or title is null or empty.</exception>
        public async Task<TodoTask?> CreateTaskAsync(string listId, string title, string? notes, bool highImportance, DateTime? reminderOn, DateTime? dueDate, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentException("List ID cannot be null or empty.", nameof(listId));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Task title cannot be null or empty.", nameof(title));

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

            try
            {
                return await CreateTaskAsync(listId, task, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task in list {ListId} with title {Title}", listId, title);
                throw;
            }
        }

        /// <summary>
        /// Creates a new task in the specified task list.
        /// </summary>
        /// <param name="listId">The ID of the task list.</param>
        /// <param name="task">The task to create.</param>
        /// <param name="linkedResource">The linked resource for the task.</param>
        /// <param name="checkListItems">The checklist items for the task.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created task.</returns>
        /// <exception cref="ArgumentException">Thrown when the listId or task is null or empty.</exception>
        public async Task<TodoTask?> CreateTaskAsync(string listId, TodoTask task, LinkedResource? linkedResource = null, IEnumerable<string>? checkListItems = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentException("List ID cannot be null or empty.", nameof(listId));
            if (task == null)
                throw new ArgumentException("Task cannot be null.", nameof(task));

            if (linkedResource != null)
                task.LinkedResources = new List<LinkedResource>() { linkedResource };
            if (checkListItems != null)
                task.ChecklistItems = checkListItems.Select(i => new ChecklistItem() { DisplayName = i }).ToList();

            try
            {
                return await _graphClient.Me.Todo.Lists[listId].Tasks.PostAsync(task, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task in list {ListId}", listId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all open tasks in the specified task list.
        /// </summary>
        /// <param name="listId">The ID of the task list.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of open tasks.</returns>
        public Task<IEnumerable<TodoTask>> GetOpenTasksAsync(string listId, CancellationToken cancellationToken = default)
        {
            return GetTasksAsync(listId, OpenTaskFilter, cancellationToken);
        }

        /// <summary>
        /// Retrieves all completed tasks in the specified task list.
        /// </summary>
        /// <param name="listId">The ID of the task list.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of completed tasks.</returns>
        public Task<IEnumerable<TodoTask>> GetCompletedTasksAsync(string listId, CancellationToken cancellationToken = default)
        {
            return GetTasksAsync(listId, CompletedTaskFilter, cancellationToken);
        }

        /// <summary>
        /// Retrieves tasks in the specified task list based on the filter expression.
        /// </summary>
        /// <param name="listId">The ID of the task list.</param>
        /// <param name="filterExpression">The OData filter expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of tasks.</returns>
        /// <exception cref="ArgumentException">Thrown when the listId is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the result of the request is null.</exception>
        public async Task<IEnumerable<TodoTask>> GetTasksAsync(string listId, string? filterExpression = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentException("List ID cannot be null or empty.", nameof(listId));

            Action<RequestConfiguration<TasksRequestBuilder.TasksRequestBuilderGetQueryParameters>> filter = default;

            if (!string.IsNullOrWhiteSpace(filterExpression))
            {
                filter = (r) => r.QueryParameters.Filter = filterExpression;
            }

            try
            {
                var result = await _graphClient.Me.Todo.Lists[listId].Tasks.GetAsync(requestConfiguration: filter, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (result == null) throw new InvalidOperationException("The result of the request is null");
                return result.Value ??= new List<TodoTask>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks from list {ListId}", listId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all tasks across all task lists based on the filter expression.
        /// </summary>
        /// <param name="filterExpression">The filter expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of task sets.</returns>
        public async Task<IEnumerable<TodoSetDto>> GetAllTasksAsync(string? filterExpression = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var lists = await GetTaskLists(null, cancellationToken).ConfigureAwait(false);
                var result = new List<TodoSetDto>();
                foreach (var list in lists)
                {
                    var tasks = await GetTasksAsync(list.Id, filterExpression, cancellationToken).ConfigureAwait(false);
                    result.Add(new TodoSetDto() { ListId = list.Id, DisplanyName = list.DisplayName, Tasks = tasks.ToList() });
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tasks");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all open tasks across all task lists.
        /// </summary>
        /// <param name="filterExpression">The filter expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of task sets.</returns>
        public async Task<IEnumerable<TodoSetDto>> GetAllOpenTasks(string? filterExpression = null, CancellationToken cancellationToken = default)
        {
            return await GetAllTasksAsync(OpenTaskFilter, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all completed tasks across all task lists.
        /// </summary>
        /// <param name="filterExpression">The filter expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of task sets.</returns>
        public async Task<IEnumerable<TodoSetDto>> GetAllCompletedTasks(string? filterExpression = null, CancellationToken cancellationToken = default)
        {
            return await GetAllTasksAsync(CompletedTaskFilter, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a checklist item to the specified task.
        /// </summary>
        /// <param name="listId">The ID of the task list.</param>
        /// <param name="taskId">The ID of the task.</param>
        /// <param name="displayName">The display name of the checklist item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentException">Thrown when the listId, taskId, or displayName is null or empty.</exception>
        public async Task AddChecklistItemAsync(string listId, string taskId, string displayName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentException("List ID cannot be null or empty.", nameof(listId));
            if (string.IsNullOrWhiteSpace(taskId))
                throw new ArgumentException("Task ID cannot be null or empty.", nameof(taskId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be null or empty.", nameof(displayName));

            try
            {
                await _graphClient.Me.Todo.Lists[listId].Tasks[taskId].ChecklistItems.PostAsync(new ChecklistItem() { DisplayName = displayName }, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding checklist item to task {TaskId} in list {ListId}", taskId, listId);
                throw;
            }
        }

        /// <summary>
        /// Adds a linked resource to the specified task.
        /// </summary>
        /// <param name="listId">The ID of the task list.</param>
        /// <param name="taskId">The ID of the task.</param>
        /// <param name="linkedResource">The linked resource to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The added linked resource.</returns>
        /// <exception cref="ArgumentException">Thrown when the listId, taskId, or linkedResource is null or empty.</exception>
        public async Task<LinkedResource?> AddLinkedResourseAsync(string listId, string taskId, LinkedResource linkedResource, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentException("List ID cannot be null or empty.", nameof(listId));
            if (string.IsNullOrWhiteSpace(taskId))
                throw new ArgumentException("Task ID cannot be null or empty.", nameof(taskId));
            if (linkedResource == null)
                throw new ArgumentException("Linked resource cannot be null.", nameof(linkedResource));

            try
            {
                return await _graphClient.Me.Todo.Lists[listId].Tasks[taskId].LinkedResources.PostAsync(linkedResource, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding linked resource to task {TaskId} in list {ListId}", taskId, listId);
                throw;
            }
        }
    }
}
