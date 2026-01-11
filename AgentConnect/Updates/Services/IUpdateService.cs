using System;
using System.Threading;
using System.Threading.Tasks;
using AgentConnect.Updates.Models;

namespace AgentConnect.Updates.Services
{
    /// <summary>
    /// Service interface for managing application updates.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// Current application version.
        /// </summary>
        string CurrentVersion { get; }

        /// <summary>
        /// Whether the application was installed via Velopack (vs running from source).
        /// </summary>
        bool IsInstalled { get; }

        /// <summary>
        /// Event raised when an update is available.
        /// </summary>
        event EventHandler<ExtendedUpdateInfo> UpdateAvailable;

        /// <summary>
        /// Event raised when update download progress changes.
        /// </summary>
        event EventHandler<int> DownloadProgressChanged;

        /// <summary>
        /// Check for available updates.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Update info if available, null otherwise.</returns>
        Task<ExtendedUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Download update in background.
        /// </summary>
        /// <param name="updateInfo">The update to download.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DownloadUpdateAsync(ExtendedUpdateInfo updateInfo,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Apply update and restart application.
        /// </summary>
        /// <param name="updateInfo">The update to apply.</param>
        void ApplyUpdateAndRestart(ExtendedUpdateInfo updateInfo);

        /// <summary>
        /// Apply update when application exits.
        /// </summary>
        /// <param name="updateInfo">The update to apply on exit.</param>
        void ApplyUpdateOnExit(ExtendedUpdateInfo updateInfo);

        /// <summary>
        /// Defer update to later.
        /// </summary>
        /// <param name="updateInfo">The update to defer.</param>
        /// <returns>True if deferral was successful, false if max deferrals reached.</returns>
        Task<bool> DeferUpdateAsync(ExtendedUpdateInfo updateInfo);

        /// <summary>
        /// Check if update can still be deferred.
        /// </summary>
        /// <param name="updateInfo">The update to check.</param>
        /// <returns>True if deferral is allowed.</returns>
        bool CanDeferUpdate(ExtendedUpdateInfo updateInfo);
    }
}
