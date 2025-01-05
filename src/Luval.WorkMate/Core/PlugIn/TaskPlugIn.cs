using Luval.WorkMate.Core.Services;
using Luval.WorkMate.Infrastructure.Data;
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
    public class TaskPlugIn
    {
        private readonly TodoService _todoService;

        public TaskPlugIn(TodoService todoService)
        {
            _todoService = todoService;
        }


        [KernelFunction("create_task_category")]
        [Description("Create a new category to assign a task, a category can be for example Personal, Work, Misc, etc")]
        public async Task<TodoTaskList?> CreateTaskCategoryAsync(string categoryName)
        {
            return await _todoService.CreateTaskListAsync(categoryName);
        }

        [KernelFunction("get_task_category_by_name")]
        [Description("Gets the task category object by the name of the category")]
        public async Task<TodoTaskList?> GetTaskCategoryByNameAsync(string categoryName)
        {
            return (await _todoService.GetTaskLists(string.Format("contains(displayName,'{0}')", categoryName))).SingleOrDefault();
        }

        [KernelFunction("create_task")]
        [Description("Creates a new task in the desired category by passing the category Id, the name of the task, the notes to describe what needs to be done, an indicator if it is of high priority which is optional and the due date that is also optional")]
        public async Task<TodoTask> CreateTaskAsync(string categoryId, string taskName, string? taskNotes, bool isHighPriority = false, DateTime? taskDueDate = null)
        {
            return await _todoService.CreateTaskAsync(categoryId, taskName, taskNotes, isHighPriority, null, taskDueDate);
        }

        public async Task<IEnumerable<TodoSetDto>> GetAllTasksAsync()
        {
            return await _todoService.GetAllTasksAsync();
        }
    }
}
