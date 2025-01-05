using Luval.AuthMate.Core.Interfaces;
using Luval.GenAIBotMate.Core.Entities;
using Luval.GenAIBotMate.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Resolver
{
    public class BotResolver
    {
        private readonly IGenAIBotStorageService _botStorageService;
        private readonly IConfiguration _config;
        private readonly ILogger<BotResolver> _logger;
        private readonly IUserResolver _userResolver;
        private readonly string _systemPrompt;

        public BotResolver(IGenAIBotStorageService botStorageService, IConfiguration config, IUserResolver userResolver, ILogger<BotResolver> logger)
        {
            _botStorageService = botStorageService ?? throw new ArgumentNullException(nameof(botStorageService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userResolver = userResolver ?? throw new ArgumentNullException(nameof(userResolver));
            var botName = _config["Bot:Name"];
            if (string.IsNullOrEmpty(botName)) throw new ArgumentNullException("The configuration for the agent in missing in Bot:Name");
            BotName = botName;
            _systemPrompt = _config["Bot:SystemPrompt"] ?? "you are a helpful agent";


        }

        public string BotName { get; private set; }
        public bool ForceCreate { get; private set; }

        public GenAIBot GetBotAsync(CancellationToken cancellationToken)
        {

        }

        private async GenAIBot CreateAsync(CancellationToken cancellationToken)
        {
            var user = _userResolver.GetUser();
            var bot = new GenAIBot()
            {
                Name = BotName,
                SystemPrompt = _systemPrompt,
                AccountId = user.AccountId,
                CreatedBy = user.Email,
                UpdatedBy = user.Email,
                UtcCreatedOn = DateTime.UtcNow,
                UtcUpdatedOn = DateTime.UtcNow
            };
            await _botStorageService.CreateChatbotAsync(bot, cancellationToken);
            return bot;
        }
    }
}
