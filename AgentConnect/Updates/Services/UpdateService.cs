using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AgentConnect.Updates.Configuration;
using AgentConnect.Updates.Models;
using Velopack;
using Velopack.Sources;

namespace AgentConnect.Updates.Services
{
    /// <summary>
    /// Core update service that integrates with Velopack for update management.
    /// </summary>
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
            _manifestService = manifestService;
            _deferralService = deferralService;
            _telemetryService = telemetryService;

            var options = new UpdateOptions
            {
                ExplicitChannel = channel ?? UpdateConstants.DefaultChannel
            };

            // Use GitHub source for AveronHQ/AgentConnect repository
            var source = new GithubSource(
                UpdateConstants.GitHubRepoUrl,
                accessToken: null, // Public repository
                prerelease: false  // Only stable releases
            );

            _updateManager = new UpdateManager(source, options);
        }

        public async Task<ExtendedUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
            {
                EventType = TelemetryEventType.UpdateCheckStarted,
                CurrentVersion = CurrentVersion
            });

            try
            {
                var updateInfo = await _updateManager.CheckForUpdatesAsync();

                if (updateInfo == null)
                {
                    await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
                    {
                        EventType = TelemetryEventType.UpdateCheckCompleted,
                        CurrentVersion = CurrentVersion,
                        Success = true
                    });
                    return null;
                }

                var targetVersion = updateInfo.TargetFullRelease.Version.ToString();

                // Fetch extended manifest from GitHub release assets
                var manifest = await _manifestService.GetManifestAsync(targetVersion, cancellationToken);

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

                // Apply deferral state
                var deferralState = await _deferralService.GetDeferralStateAsync(targetVersion);
                if (deferralState != null)
                {
                    extendedInfo.DeferUntil = deferralState.DeferUntil;

                    // Check if deferral has expired
                    if (deferralState.DeferUntil.HasValue && DateTime.UtcNow < deferralState.DeferUntil.Value)
                    {
                        // Still in deferral period, don't notify yet
                        return null;
                    }
                }

                await _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
                {
                    EventType = TelemetryEventType.UpdateAvailable,
                    CurrentVersion = CurrentVersion,
                    TargetVersion = targetVersion,
                    UpdateType = extendedInfo.Type,
                    Success = true
                });

                UpdateAvailable?.Invoke(this, extendedInfo);
                return extendedInfo;
            }
            catch (Exception ex)
            {
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
            var stopwatch = Stopwatch.StartNew();
            var targetVersion = updateInfo.TargetVersion;

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
                        progress?.Report(p);
                        DownloadProgressChanged?.Invoke(this, p);
                    });

                stopwatch.Stop();

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
            _telemetryService.TrackEventAsync(new UpdateTelemetryEvent
            {
                EventType = TelemetryEventType.UpdateApplyStarted,
                CurrentVersion = CurrentVersion,
                TargetVersion = updateInfo.TargetVersion
            }).Wait();

            // Clear deferral state since update is being applied
            _deferralService.ClearDeferralAsync(updateInfo.TargetVersion).Wait();

            _updateManager.ApplyUpdatesAndRestart(updateInfo.VelopackUpdateInfo);
        }

        public void ApplyUpdateOnExit(ExtendedUpdateInfo updateInfo)
        {
            // Clear deferral state since update will be applied
            _deferralService.ClearDeferralAsync(updateInfo.TargetVersion).Wait();

            _updateManager.WaitExitThenApplyUpdates(updateInfo.VelopackUpdateInfo);
        }

        public async Task<bool> DeferUpdateAsync(ExtendedUpdateInfo updateInfo)
        {
            if (!CanDeferUpdate(updateInfo))
                return false;

            var result = await _deferralService.DeferUpdateAsync(
                updateInfo.TargetVersion,
                updateInfo.MaxDeferrals);

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
            // Cannot defer forced or security updates
            if (updateInfo.Type == UpdateType.Forced || updateInfo.Type == UpdateType.SecurityHotfix)
                return false;

            // Silent updates don't need deferral (they're automatic)
            if (updateInfo.Type == UpdateType.Silent)
                return false;

            return _deferralService.CanDeferAsync(
                updateInfo.TargetVersion,
                updateInfo.MaxDeferrals,
                updateInfo.MinutesUntilForced).Result;
        }
    }
}
