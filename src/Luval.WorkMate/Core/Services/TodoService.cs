using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Services
{
    public class TodoService
    {
        public async Task CreateTask(string title, CancellationToken cancellationToken = default)
        {
            var client = Microsoft.Graph.GraphClientFactory.Create();
        }
    }
}
