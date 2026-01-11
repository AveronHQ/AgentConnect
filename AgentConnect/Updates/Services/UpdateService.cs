using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AgentConnect.Services;
using AgentConnect.Updates.Configuration;
using AgentConnect.Updates.Models;
using Velopack;
using Velopack.Sources;

namespace AgentConnect.Updates.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly UpdateManager _updateManager;
        private readonly IUpdateManifestService _manifestService;
        private readonly IDeferralService _deferralService;
        private readonly IUpdateTelemetryService _telemetryService;

        public string CurrentVersion => _updateManager?.CurrentVersion?.ToString() ?? "0.0.0.0";
        public bool IsInstalled => _updateManager?.IsInstalled ?? false;

        public event EventHandler<ExtendedUpdateInfo> UpdateAvailable;
        public event EventHandler<int> DownloadProgressChanged;

        public UpdateService(
            IUpdateManifestService manifestService,
            IDeferralService deferralService,
            IUpdateTelemetryService telemetryService,
            string channel = null)
        {
            UpdateLogger.Log("UpdateService", "=== CONSTRUCTOR START ===");
            UpdateLogger.Log("UpdateService", $"Channel param: {channel ?? "(null)"}");

            _manifestService = manifestService;
            _deferralService = deferralService;
            _telemetryService = telemetryService;

            try
            {
                var options = new UpdateOptions
                {
                    ExplicitChannel = channel ?? UpdateConstants.DefaultChannel
                };
                UpdateLogger.Log("UpdateService", $"UpdateOptions.ExplicitChannel: {options.ExplicitChannel}");

                UpdateLogger.Log("UpdateService", $"Creating GithubSource with URL: {UpdateConstants.GitHubRepoUrl}");
                var source = new GithubSource(
                    UpdateConstants.GitHubRepoUrl,
                    accessToken: null,
                    prerelease: false
                );
                UpdateLogger.Log("UpdateService", "GithubSource created successfully");

                UpdateLogger.Log("UpdateService", "Creating UpdateManager...");
                _updateManager = new UpdateManager(source, options);
                UpdateLogger.Log("UpdateService", "UpdateManager created successfully");

                UpdateLogger.Log("UpdateService", $"IsInstalled: {IsInstalled}");
                UpdateLogger.Log("UpdateService", $"CurrentVersion: {CurrentVersion}");
            }
            catch (Exception ex)
            {
                UpdateLogger.LogException("UpdateService", ex);
                throw;
            }

            UpdateLogger.Log("UpdateService", "=== CONSTRUCTOR END ===");
        }

        public async Task<ExtendedUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            UpdateLogger.Log("UpdateService", "=== CheckForUpdatesAsync START ===");
            UpdateLogger.Log("UpdateService", $"CurrentVersion: {CurrentVersion}");
            UpdateLogger.Log("UpdateService", $"IsInstalled: {IsInstalled}");

            await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
            {
                EventType = TelemetryEventType.UpdateCheckStarted,
                CurrentVersion = CurrentVersion
            });

            try
            {
                UpdateLogger.Log("UpdateService", "Calling _updateManager.CheckForUpdatesAsync()...");
                var updateInfo = await _updateManager.CheckForUpdatesAsync();

                if (updateInfo == null)
                {
                    UpdateLogger.Log("UpdateService", "CheckForUpdatesAsync returned NULL - no updates available");
                    await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
                    {
                        EventType = TelemetryEventType.UpdateCheckCompleted,
                        CurrentVersion = CurrentVersion,
                        Success = true
                    });
                    return null;
                }

                UpdateLogger.Log("UpdateService", "UPDATE FOUND!");
                UpdateLogger.Log("UpdateService", $"TargetFullRelease.Version: {updateInfo.TargetFullRelease?.Version}");
                UpdateLogger.Log("UpdateService", $"TargetFullRelease.FileName: {updateInfo.TargetFullRelease?.FileName}");
                UpdateLogger.Log("UpdateService", $"IsDowngrade: {updateInfo.IsDowngrade}");

                var targetVersion = updateInfo.TargetFullRelease.Version.ToString();
                UpdateLogger.Log("UpdateService", $"Target version string: {targetVersion}");

                UpdateLogger.Log("UpdateService", "Fetching manifest...");
                var manifest = await _manifestService.GetManifestAsync(targetVersion, cancellationToken);
                UpdateLogger.Log("UpdateService", $"Manifest fetched: {(manifest != null ? "SUCCESS" : "NULL")}");
                if (manifest != null)
                {
                    UpdateLogger.Log("UpdateService", $"Manifest.Type: {manifest.Type}");
                    UpdateLogger.Log("UpdateService", $"Manifest.ReleaseNotes: {manifest.ReleaseNotes}");
                    UpdateLogger.Log("UpdateService", $"Manifest.MaxDeferrals: {manifest.MaxDeferrals}");
                }

                var extendedInfo = new ExtendedUpdateInfo
                {
                    VelopackUpdateInfo = updateInfo,
                    Type = manifest?.Type ?? UpdateType.Prompted,
                    ReleaseNotes = manifest?.ReleaseNotes ?? "Update available.",
                    ReleaseNotesUrl = manifest?.ReleaseNotesUrl ??
                        $"{UpdateConstants.GitHubRepoUrl}/releases/tag/v{targetVersion}",
                    MaxDeferrals = manifest?.MaxDeferrals ?? UpdateConstants.MaxDeferralsDefault,
                    MinutesUntilForced = manifest?.MinutesUntilForced ?? UpdateConstants.MinutesUntilForcedDefault
                };

                UpdateLogger.Log("UpdateService", $"ExtendedInfo.Type: {extendedInfo.Type}");

                UpdateLogger.Log("UpdateService", "Checking deferral state...");
                var deferralState = await _deferralService.GetDeferralStateAsync(targetVersion);
                if (deferralState != null)
                {
                    UpdateLogger.Log("UpdateService", $"Deferral state found - DeferUntil: {deferralState.DeferUntil}");
                    UpdateLogger.Log("UpdateService", $"Deferral state - DeferralCount: {deferralState.DeferralCount}");
                    extendedInfo.DeferUntil = deferralState.DeferUntil;

                    if (deferralState.DeferUntil.HasValue && DateTime.UtcNow < deferralState.DeferUntil.Value)
                    {
                        UpdateLogger.Log("UpdateService", "Still in deferral period - returning null");
                        return null;
                    }
                }
                else
                {
                    UpdateLogger.Log("UpdateService", "No deferral state found");
                }

                await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
                {
                    EventType = TelemetryEventType.UpdateAvailable,
                    CurrentVersion = CurrentVersion,
                    TargetVersion = targetVersion,
                    UpdateType = extendedInfo.Type,
                    Success = true
                });

                UpdateLogger.Log("UpdateService", "Invoking UpdateAvailable event...");
                UpdateAvailable?.Invoke(this, extendedInfo);
                UpdateLogger.Log("UpdateService", "=== CheckForUpdatesAsync END - returning extendedInfo ===");
                return extendedInfo;
            }
            catch (Exception ex)
            {
                UpdateLogger.LogException("UpdateService", ex);
                await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
                {
                    EventType = TelemetryEventType.UpdateCheckCompleted,
                    CurrentVersion = CurrentVersion,
                    Success = false,
                    FailureReason = ex.GetType().Name,
                    FailureDetails = ex.Message
                });
                throw;
            }
        }

        public async Task DownloadUpdateAsync(ExtendedUpdateInfo updateInfo,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            UpdateLogger.Log("UpdateService", "=== DownloadUpdateAsync START ===");
            var stopwatch = Stopwatch.StartNew();
            var targetVersion = updateInfo.TargetVersion;
            UpdateLogger.Log("UpdateService", $"Downloading version: {targetVersion}");

            await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
            {
                EventType = TelemetryEventType.UpdateDownloadStarted,
                CurrentVersion = CurrentVersion,
                TargetVersion = targetVersion
            });

            try
            {
                await _updateManager.DownloadUpdatesAsync(
                    updateInfo.VelopackUpdateInfo,
                    p =>
                    {
                        UpdateLogger.Log("UpdateService", $"Download progress: {p}%");
                        progress?.Report(p);
                        DownloadProgressChanged?.Invoke(this, p);
                    });

                stopwatch.Stop();
                UpdateLogger.Log("UpdateService", $"Download completed in {stopwatch.ElapsedMilliseconds}ms");

                await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
                {
                    EventType = TelemetryEventType.UpdateDownloadCompleted,
                    CurrentVersion = CurrentVersion,
                    TargetVersion = targetVersion,
                    Success = true,
                    DownloadTimeMs = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                UpdateLogger.LogException("UpdateService", ex);
                await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
                {
                    EventType = TelemetryEventType.UpdateDownloadFailed,
                    CurrentVersion = CurrentVersion,
                    TargetVersion = targetVersion,
                    Success = false,
                    FailureReason = ex.GetType().Name,
                    FailureDetails = ex.Message
                });
                throw;
            }
        }

        public void ApplyUpdateAndRestart(ExtendedUpdateInfo updateInfo)
        {
            UpdateLogger.Log("UpdateService", "=== ApplyUpdateAndRestart ===");
            UpdateLogger.Log("UpdateService", $"Applying version: {updateInfo.TargetVersion}");

            _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
            {
                EventType = TelemetryEventType.UpdateApplyStarted,
                CurrentVersion = CurrentVersion,
                TargetVersion = updateInfo.TargetVersion
            }).Wait();

            _deferralService.ClearDeferralAsync(updateInfo.TargetVersion).Wait();

            UpdateLogger.Log("UpdateService", "Calling ApplyUpdatesAndRestart...");
            _updateManager.ApplyUpdatesAndRestart(updateInfo.VelopackUpdateInfo);
        }

        public void ApplyUpdateOnExit(ExtendedUpdateInfo updateInfo)
        {
            UpdateLogger.Log("UpdateService", "=== ApplyUpdateOnExit ===");
            UpdateLogger.Log("UpdateService", $"Will apply version on exit: {updateInfo.TargetVersion}");

            _deferralService.ClearDeferralAsync(updateInfo.TargetVersion).Wait();

            _updateManager.WaitExitThenApplyUpdates(updateInfo.VelopackUpdateInfo);
            UpdateLogger.Log("UpdateService", "WaitExitThenApplyUpdates called");
        }

        public async Task<bool> DeferUpdateAsync(ExtendedUpdateInfo updateInfo)
        {
            UpdateLogger.Log("UpdateService", "=== DeferUpdateAsync ===");
            if (!CanDeferUpdate(updateInfo))
            {
                UpdateLogger.Log("UpdateService", "Cannot defer this update");
                return false;
            }

            var result = await _deferralService.DeferUpdateAsync(
                updateInfo.TargetVersion,
                updateInfo.MaxDeferrals);

            UpdateLogger.Log("UpdateService", $"Deferral result: {result}");

            if (result)
            {
                var state = await _deferralService.GetDeferralStateAsync(updateInfo.TargetVersion);
                await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
                {
                    EventType = TelemetryEventType.UpdateDeferred,
                    CurrentVersion = CurrentVersion,
                    TargetVersion = updateInfo.TargetVersion,
                    DeferralCount = state?.DeferralCount ?? 1
                });
            }

            return result;
        }

        public bool CanDeferUpdate(ExtendedUpdateInfo updateInfo)
        {
            if (updateInfo.Type == UpdateType.Forced || updateInfo.Type == UpdateType.SecurityHotfix)
                return false;

            if (updateInfo.Type == UpdateType.Silent)
                return false;

            return _deferralService.CanDeferAsync(
                updateInfo.TargetVersion,
                updateInfo.MaxDeferrals,
                updateInfo.MinutesUntilForced).Result;
        }
    }
}
