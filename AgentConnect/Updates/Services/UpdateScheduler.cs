using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using AgentConnect.Services;
using AgentConnect.Updates.Configuration;
using AgentConnect.Updates.Models;
using Timer = System.Timers.Timer;

namespace AgentConnect.Updates.Services
{
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
            UpdateLogger.Log("UpdateScheduler", "=== CONSTRUCTOR ===");
            _updateService = updateService;
            _dispatcher = dispatcher;
            _checkInterval = TimeSpan.FromHours(UpdateConstants.UpdateCheckIntervalHours);
            _timer = new Timer(_checkInterval.TotalMilliseconds);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            UpdateLogger.Log("UpdateScheduler", $"Check interval: {_checkInterval}");
        }

        public void Start()
        {
            UpdateLogger.Log("UpdateScheduler", "=== Start() called ===");

            if (_isRunning)
            {
                UpdateLogger.Log("UpdateScheduler", "Already running, returning");
                return;
            }

            _isRunning = true;
            _cts = new CancellationTokenSource();

            UpdateLogger.Log("UpdateScheduler", "Scheduling immediate update check via Task.Run...");
            Task.Run(async () =>
            {
                UpdateLogger.Log("UpdateScheduler", "Task.Run started - calling CheckNowAsync");
                await CheckNowAsync();
                UpdateLogger.Log("UpdateScheduler", "Task.Run completed");
            });

            _timer.Start();
            UpdateLogger.Log("UpdateScheduler", "Timer started");
        }

        public void Stop()
        {
            UpdateLogger.Log("UpdateScheduler", "=== Stop() called ===");
            _isRunning = false;
            _timer.Stop();
            _cts?.Cancel();
        }

        public async Task CheckNowAsync()
        {
            UpdateLogger.Log("UpdateScheduler", "=== CheckNowAsync START ===");
            UpdateLogger.Log("UpdateScheduler", $"IsInstalled: {_updateService.IsInstalled}");

            if (!_updateService.IsInstalled)
            {
                UpdateLogger.Log("UpdateScheduler", "NOT INSTALLED - skipping update check (running from source)");
                return;
            }

            try
            {
                UpdateLogger.Log("UpdateScheduler", "Calling _updateService.CheckForUpdatesAsync...");
                var updateInfo = await _updateService.CheckForUpdatesAsync(_cts?.Token ?? CancellationToken.None);

                UpdateLogger.Log("UpdateScheduler", $"CheckForUpdatesAsync returned: {(updateInfo != null ? "UPDATE INFO" : "NULL")}");

                if (updateInfo != null)
                {
                    UpdateLogger.Log("UpdateScheduler", $"Update found - Type: {updateInfo.Type}, Version: {updateInfo.TargetVersion}");
                    await HandleUpdateAsync(updateInfo);
                }
                else
                {
                    UpdateLogger.Log("UpdateScheduler", "No update available");
                }
            }
            catch (OperationCanceledException)
            {
                UpdateLogger.Log("UpdateScheduler", "Operation cancelled");
            }
            catch (Exception ex)
            {
                UpdateLogger.LogException("UpdateScheduler", ex);
            }

            UpdateLogger.Log("UpdateScheduler", "=== CheckNowAsync END ===");
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            UpdateLogger.Log("UpdateScheduler", "Timer elapsed - checking for updates");
            await CheckNowAsync();
        }

        private async Task HandleUpdateAsync(ExtendedUpdateInfo updateInfo)
        {
            UpdateLogger.Log("UpdateScheduler", "=== HandleUpdateAsync ===");
            UpdateLogger.Log("UpdateScheduler", $"Update type: {updateInfo.Type}");

            switch (updateInfo.Type)
            {
                case UpdateType.Silent:
                    UpdateLogger.Log("UpdateScheduler", "Handling as SILENT update");
                    await HandleSilentUpdateAsync(updateInfo);
                    break;

                case UpdateType.Prompted:
                case UpdateType.Forced:
                case UpdateType.SecurityHotfix:
                    UpdateLogger.Log("UpdateScheduler", $"Handling as {updateInfo.Type} update - invoking UI event");
                    _dispatcher.Invoke(() =>
                    {
                        UpdateLogger.Log("UpdateScheduler", "Inside Dispatcher.Invoke - raising UpdateAvailable event");
                        UpdateLogger.Log("UpdateScheduler", $"UpdateAvailable event has {(UpdateAvailable != null ? "subscribers" : "NO SUBSCRIBERS")}");
                        UpdateAvailable?.Invoke(this, updateInfo);
                        UpdateLogger.Log("UpdateScheduler", "UpdateAvailable event raised");
                    });
                    break;
            }
        }

        private async Task HandleSilentUpdateAsync(ExtendedUpdateInfo updateInfo)
        {
            UpdateLogger.Log("UpdateScheduler", "=== HandleSilentUpdateAsync ===");

            try
            {
                UpdateLogger.Log("UpdateScheduler", "Downloading update in background...");
                await _updateService.DownloadUpdateAsync(
                    updateInfo,
                    cancellationToken: _cts?.Token ?? CancellationToken.None);

                _pendingSilentUpdate = updateInfo;

                UpdateLogger.Log("UpdateScheduler", "Calling ApplyUpdateOnExit...");
                _updateService.ApplyUpdateOnExit(updateInfo);

                UpdateLogger.Log("UpdateScheduler", $"Silent update downloaded: {updateInfo.TargetVersion}. Will apply on exit.");
            }
            catch (Exception ex)
            {
                UpdateLogger.LogException("UpdateScheduler", ex);
            }
        }

        public void Dispose()
        {
            UpdateLogger.Log("UpdateScheduler", "=== Dispose() ===");
            Stop();
            _timer?.Dispose();
            _cts?.Dispose();
        }
    }
}
