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
    /// <summary>
    /// Resolves the bot instance for the current user.
    /// </summary>
    public class BotResolver
    {
        private readonly IGenAIBotStorageService _botStorageService;
        private readonly IConfiguration _config;
        private readonly ILogger<BotResolver> _logger;
        private readonly IUserResolver _userResolver;
        private readonly string _systemPrompt;
        private static Dictionary<ulong, GenAIBot> _bots = new Dictionary<ulong, GenAIBot>();

        /// <summary>
        /// Initializes a new instance of the <see cref="BotResolver"/> class.
        /// </summary>
        /// <param name="botStorageService">The bot storage service.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="userResolver">The user resolver.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="botStorageService"/>, <paramref name="config"/>, <paramref name="userResolver"/>, or <paramref name="logger"/> is null.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown when the configuration for the agent is missing in Bot:Name.</exception>
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

        /// <summary>
        /// Gets the name of the bot.
        /// </summary>
        public string BotName { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to force create a new bot.
        /// </summary>
        public bool ForceCreate { get; private set; }

        /// <summary>
        /// Gets the bot instance for the current user.
        /// </summary>
        private GenAIBot Bot => Get();

        /// <summary>
        /// Gets the bot instance for the current user.
        /// </summary>
        /// <returns>The bot instance.</returns>
        private GenAIBot Get()
        {
            try
            {
                var userId = _userResolver.GetUser().Id;
                if (_bots.ContainsKey(userId))
                    return _bots[userId];
                _bots[userId] = GetBotAsync().GetAwaiter().GetResult();
                return _bots[userId];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting the bot.");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously gets the bot instance for the current user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The bot instance.</returns>
        private async Task<GenAIBot> GetBotAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                GenAIBot bot = default!;
                if (ForceCreate)
                {
                    bot = await CreateAsync(cancellationToken);
                }
                else
                {
                    bot = await _botStorageService.GetChatbotAsync(BotName, cancellationToken);
                    if (bot == null)
                        bot = await CreateAsync(cancellationToken);
                }
                return bot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while asynchronously getting the bot.");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously creates a new bot instance.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created bot instance.</returns>
        private async Task<GenAIBot> CreateAsync(CancellationToken cancellationToken = default)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating the bot.");
                throw;
            }
        }
    }
}
