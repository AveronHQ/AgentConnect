using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using AgentConnect.Updates.Configuration;
using AgentConnect.Updates.Models;
using Timer = System.Timers.Timer;

namespace AgentConnect.Updates.Services
{
    /// <summary>
    /// Scheduler for automatic update checks.
    /// Checks immediately on app launch and then every 24 hours.
    /// </summary>
    public class UpdateScheduler : IUpdateScheduler
    {
        private readonly IUpdateService _updateService;
        private readonly Dispatcher _dispatcher;
        private readonly Timer _timer;
        private readonly TimeSpan _checkInterval;
        private bool _isRunning;
        private CancellationTokenSource _cts;
        private ExtendedUpdateInfo _pendingSilentUpdate;

        public IUpdateService UpdateService => _updateService;

        public event EventHandler<ExtendedUpdateInfo> UpdateAvailable;

        public UpdateScheduler(IUpdateService updateService, Dispatcher dispatcher)
        {
            _updateService = updateService;
            _dispatcher = dispatcher;
            _checkInterval = TimeSpan.FromHours(UpdateConstants.UpdateCheckIntervalHours);
            _timer = new Timer(_checkInterval.TotalMilliseconds);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
        }

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cts = new CancellationTokenSource();

            // Check immediately on app launch
            Task.Run(async () => await CheckNowAsync());

            // Start periodic timer
            _timer.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _timer.Stop();
            _cts?.Cancel();
        }

        public async Task CheckNowAsync()
        {
            if (!_updateService.IsInstalled)
            {
                // Don't check for updates when running from source
                return;
            }

            try
            {
                var updateInfo = await _updateService.CheckForUpdatesAsync(_cts?.Token ?? CancellationToken.None);

                if (updateInfo != null)
                {
                    await HandleUpdateAsync(updateInfo);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelled, ignore
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            await CheckNowAsync();
        }

        private async Task HandleUpdateAsync(ExtendedUpdateInfo updateInfo)
        {
            switch (updateInfo.Type)
            {
                case UpdateType.Silent:
                    await HandleSilentUpdateAsync(updateInfo);
                    break;

                case UpdateType.Prompted:
                case UpdateType.Forced:
                case UpdateType.SecurityHotfix:
                    // Raise event for UI to handle
                    _dispatcher.Invoke(() =>
                    {
                        UpdateAvailable?.Invoke(this, updateInfo);
                    });
                    break;
            }
        }

        private async Task HandleSilentUpdateAsync(ExtendedUpdateInfo updateInfo)
        {
            try
            {
                // Download in background
                await _updateService.DownloadUpdateAsync(
                    updateInfo,
                    cancellationToken: _cts?.Token ?? CancellationToken.None);

                // Store for apply on exit
                _pendingSilentUpdate = updateInfo;

                // Apply on next restart
                _updateService.ApplyUpdateOnExit(updateInfo);

                System.Diagnostics.Debug.WriteLine(
                    $"Silent update downloaded: {updateInfo.TargetVersion}. Will apply on exit.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Silent update failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            _timer?.Dispose();
            _cts?.Dispose();
        }
    }
}
