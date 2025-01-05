using Luval.WorkMate.Core.Services;
using Luval.WorkMate.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.PlugIn
{
    /// <summary>
    /// This class contains the functions to interact with the tasks in the system
    /// </summary>
    public class TodoTaskPlugIn
    {
        private readonly TodoService _todoService;
        private readonly ILogger<TodoTaskPlugIn> _logger;

        public TodoTaskPlugIn(TodoService todoService, ILogger<TodoTaskPlugIn> logger)
        {
            _todoService = todoService;
            _logger = logger;
        }

        [KernelFunction("create_task_category")]
        [Description("Create a new category to assign a task, a category can be for example Personal, Work, Misc, etc")]
        public async Task<TodoTaskList?> CreateTaskCategoryAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                _logger.LogError("Category name cannot be null or empty.");
                throw new ArgumentException("Category name cannot be null or empty.", nameof(categoryName));
            }

            try
            {
                return await _todoService.CreateTaskListAsync(categoryName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task category.");
                throw;
            }
        }

        [KernelFunction("get_task_category_by_name")]
        [Description("Gets the task category object by the name of the category")]
        public async Task<TodoTaskList?> GetTaskCategoryByNameAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                _logger.LogError("Category name cannot be null or empty.");
                throw new ArgumentException("Category name cannot be null or empty.", nameof(categoryName));
            }

            try
            {
                return (await _todoService.GetTaskLists(string.Format("contains(displayName,'{0}')", categoryName))).SingleOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task category by name.");
                throw;
            }
        }

        [KernelFunction("create_task")]
        [Description("Creates a new task in the desired category by passing the category Id, the name of the task, the notes to describe what needs to be done, an indicator if it is of high priority which is optional and the due date that is also optional")]
        public async Task<TodoTask> CreateTaskAsync(string categoryId, string taskName, string? taskNotes, bool isHighPriority = false, DateTime? taskDueDate = null)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                _logger.LogError("Category ID cannot be null or empty.");
                throw new ArgumentException("Category ID cannot be null or empty.", nameof(categoryId));
            }

            if (string.IsNullOrWhiteSpace(taskName))
            {
                _logger.LogError("Task name cannot be null or empty.");
                throw new ArgumentException("Task name cannot be null or empty.", nameof(taskName));
            }

            try
            {
                return await _todoService.CreateTaskAsync(categoryId, taskName, taskNotes, isHighPriority, null, taskDueDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task.");
                throw;
            }
        }

        [KernelFunction("get_all_tasks")]
        [Description("Gets all the tasks that are in the system")]
        public async Task<IEnumerable<TodoSetDto>> GetAllTasksAsync()
        {
            try
            {
                return await _todoService.GetAllTasksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tasks.");
                throw;
            }
        }

        [KernelFunction("get_open_tasks")]
        [Description("Gets all the open tasks that are in the system")]
        public async Task<IEnumerable<TodoSetDto>> GetOpenTasksAsync()
        {
            try
            {
                return await _todoService.GetAllTasksAsync(TodoService.OpenTaskFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting open tasks.");
                throw;
            }
        }

        [KernelFunction("get_completed_tasks")]
        [Description("Gets all the completed tasks that are in the system")]
        public async Task<IEnumerable<TodoSetDto>> GetCompletedTasksAsync()
        {
            try
            {
                return await _todoService.GetAllTasksAsync(TodoService.CompletedTaskFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed tasks.");
                throw;
            }
        }
    }
}
