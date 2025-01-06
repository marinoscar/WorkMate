using Luval.GenAIBotMate.Core.Services;
using Luval.GenAIBotMate.Infrastructure.Interfaces;
using Luval.WorkMate.Core.PlugIn;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Agent
{
    public class WorkMateAgent : GenAIBotService
    {
        private readonly ILogger<GenAIBotService> _logger;
        private readonly TodoTaskPlugIn _plugIn;
        private static Kernel _kernel;
        public WorkMateAgent(IKernelBuilder kernelBuilder, TodoTaskPlugIn plugIn,  IGenAIBotStorageService storageService, IMediaService mediaService, ILoggerFactory loggerFactory) : base(GetChatCompletions(kernelBuilder, plugIn), storageService, mediaService, loggerFactory.CreateLogger<GenAIBotService>())
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<GenAIBotService>();
            if(_kernel != null)
                SetKernel(_kernel);
        }

        private static IChatCompletionService GetChatCompletions(IKernelBuilder kernelBuilder, TodoTaskPlugIn plugIn)
        {
            kernelBuilder.Plugins.AddFromObject(plugIn, "Todo");
            var kernel = kernelBuilder.Build();
            _kernel = kernel;
            return kernel.GetRequiredService<IChatCompletionService>();
        }
    }
}
