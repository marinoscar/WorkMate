using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Infrastructure.Data
{
    public class TodoSetDto
    {
        public string ListId { get; set; } = default!;
        public string DisplanyName { get; set; } = default!;
        public List<TodoTask> Tasks { get; set; } = new List<TodoTask>();
    }
}
