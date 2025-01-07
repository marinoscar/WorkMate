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
            Logger = loggerFactory.CreateLogger(GetType().Name);
            Logger.LogDebug($"{GetType().Name} initialized.");
        }

        /// <summary>
        /// Gets the service provider instance.
        /// </summary>
        protected virtual IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Gets the service scope instance.
        /// </summary>
        protected virtual IServiceScope ServiceScope { get; private set; } = default!;

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        protected virtual ILogger Logger { get; private set; } = default!;


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
        protected virtual TimeSpan GetInterval()
        {
            return GetTimeSpan("Interval", TimeSpan.FromMinutes(30));
        }

        /// <summary>
        /// Retrieves the startup delay time span for the hosted service from the configuration.
        /// </summary>
        /// <remarks>
        /// This method reads the configuration section specific to the hosted service category
        /// and retrieves the startup delay value. The interval value is expected to be a string
        /// that can be parsed into a <see cref="TimeSpan"/>. If the configuration section or
        /// the startup delay value is not found, an <see cref="ArgumentException"/> is thrown.
        /// </remarks>
        /// <returns>The interval as a <see cref="TimeSpan"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the configuration section or interval value is not found.</exception>
        protected virtual TimeSpan GetDelay()
        {
            return GetTimeSpan("StartupDelay", TimeSpan.FromMinutes(30));
        }

        protected virtual TimeSpan GetTimeSpan(string item, TimeSpan defaultValue)
        {
            Logger.LogInformation($"Retrieving {item} for hosted service type: {_category}");

            var section = _configuration.GetSection($"HostedService:{_category}");
            if (!section.GetChildren().Any())
            {
                Logger.LogError("No configuration found in HostedService section for {Category}", _category);
                throw new ArgumentException($"No configuration found in HostedService section for {_category}");
            }

            var timeSpan = section.GetValue<string>(item);
            if (string.IsNullOrWhiteSpace(timeSpan))
            {
                Logger.LogError($"No {timeSpan} found in HostedService section for {_category}");
                throw new ArgumentException($"No {item} found in HostedService section for {_category}");
            }

            Logger.LogInformation($"Timesap retrieved for {item}: {timeSpan}");

            return TimeSpan.TryParse(timeSpan, out var result) ? result : defaultValue;
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
            Logger.LogInformation($"{GetType().Name}  is running.");

            var delay = GetDelay();
            var interval = GetInterval();
            _timer = new Timer(TimerCallback, stoppingToken, delay, interval);
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

            await DoWorkAsync(stoppingToken);

            Logger.LogDebug($"{GetType().Name} is working. Count: {count} next run in {GetInterval()}");

            if (count > ulong.MaxValue - 100) count = 0;
        }

        /// <summary>
        /// The method that will be executed when the timer ticks.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public abstract Task DoWorkAsync(CancellationToken cancellationToken);

        public Task StopAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Timed Hosted Service is stopping.");

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
