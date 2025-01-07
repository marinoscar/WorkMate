using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Luval.WorkMate.Core.HostedService
{
    /// <summary>
    /// Base class for implementing a timed hosted service.
    /// </summary>
    public abstract class TimedHostedService : IHostedService, IDisposable
    {
        private ulong executionCount = 0;
        private ILogger _logger = default!;
        private IConfiguration _configuration = default!;
        private Timer? _timer = null;
        private string _category = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedHostedService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the service provider is null.</exception>
        public TimedHostedService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private void Initialize()
        {
            var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
            _configuration = ServiceProvider.GetRequiredService<IConfiguration>();

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _category = GetType().Name;
            _logger = loggerFactory.CreateLogger(GetType().Name);
        }

        /// <summary>
        /// Gets the service provider instance.
        /// </summary>
        protected virtual IServiceProvider ServiceProvider { get; private set; }

        protected virtual IServiceScope ServiceScope { get; private set; } = default!;


        /// <summary>
        /// Retrieves the interval time span for the hosted service from the configuration.
        /// </summary>
        /// <remarks>
        /// This method reads the configuration section specific to the hosted service category
        /// and retrieves the interval value. The interval value is expected to be a string
        /// that can be parsed into a <see cref="TimeSpan"/>. If the configuration section or
        /// the interval value is not found, an <see cref="ArgumentException"/> is thrown.
        /// </remarks>
        /// <returns>The interval as a <see cref="TimeSpan"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the configuration section or interval value is not found.</exception>
        protected virtual TimeSpan GetTimeSpan()
        {
            _logger.LogInformation("Retrieving interval for hosted service type: {Category}", _category);

            var section = _configuration.GetSection($"HostedService:{_category}");
            if (!section.GetChildren().Any())
            {
                _logger.LogError("No configuration found in HostedService section for {Category}", _category);
                throw new ArgumentException($"No configuration found in HostedService section for {_category}");
            }

            var interval = section.GetValue<string>("interval");
            if (string.IsNullOrWhiteSpace(interval))
            {
                _logger.LogError("No interval found in HostedService section for {Category}", _category);
                throw new ArgumentException($"No interval found in HostedService section for {_category}");
            }

            _logger.LogInformation("Interval retrieved: {Interval}", interval);
            return TimeSpan.Parse(interval);
        }

        /// <summary>
        /// Starts the timed hosted service.
        /// </summary>
        /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        public virtual async Task StartAsync(CancellationToken stoppingToken)
        {
            ServiceScope = ServiceProvider.CreateScope();
            Initialize();
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(TimerCallback, stoppingToken, TimeSpan.FromSeconds(30), GetTimeSpan());
            await OnTickAsync(stoppingToken);
        }

        /// <summary>
        /// The method that will be executed when the timer ticks.
        /// </summary>
        /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        private async Task OnTickAsync(CancellationToken stoppingToken)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogDebug("Timed Hosted Service is working. Count: {Count}", count);
            await DoWorkAsync(stoppingToken);

            if (count > ulong.MaxValue - 100) count = 0;
        }

        /// <summary>
        /// The method that will be executed when the timer ticks.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public abstract Task DoWorkAsync(CancellationToken cancellationToken);

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Dispose();

            return Task.CompletedTask;
        }

        private void InvokeSync(Func<Task> run)
        {
            Task.Run(async () => await run()).GetAwaiter().GetResult();
        }

        private void TimerCallback(object? state)
        {
            if (state == null) state = CancellationToken.None;
            InvokeSync(() => OnTickAsync((CancellationToken)state));
        }

        /// <summary>
        /// Disposes the hosted service.
        /// </summary>
        public void Dispose()
        {
            ServiceScope?.Dispose();
            _timer?.Dispose();
        }
    }
}
