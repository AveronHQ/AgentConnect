using System;

namespace AgentConnect.Updates.Models
{
    /// <summary>
    /// Types of telemetry events that can be tracked.
    /// </summary>
    public enum TelemetryEventType
    {
        UpdateCheckStarted,
        UpdateCheckCompleted,
        UpdateAvailable,
        UpdateDownloadStarted,
        UpdateDownloadProgress,
        UpdateDownloadCompleted,
        UpdateDownloadFailed,
        UpdateApplyStarted,
        UpdateApplyCompleted,
        UpdateApplyFailed,
        UpdateDeferred,
        UpdateDeferralExpired
    }

    /// <summary>
    /// Represents a telemetry event for update tracking.
    /// Note: Telemetry is currently disabled but the model is ready for future use.
    /// </summary>
    public class UpdateTelemetryEvent
    {
        /// <summary>
        /// Unique identifier for this event.
        /// </summary>
        public Guid EventId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// When the event occurred (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The type of event.
        /// </summary>
        public TelemetryEventType EventType { get; set; }

        /// <summary>
        /// The current application version.
        /// </summary>
        public string CurrentVersion { get; set; }

        /// <summary>
        /// The target version being updated to (if applicable).
        /// </summary>
        public string TargetVersion { get; set; }

        /// <summary>
        /// The update channel (e.g., "stable").
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// The type of update being applied.
        /// </summary>
        public UpdateType? UpdateType { get; set; }

        /// <summary>
        /// Whether the operation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error type if operation failed.
        /// </summary>
        public string FailureReason { get; set; }

        /// <summary>
        /// Detailed error message if operation failed.
        /// </summary>
        public string FailureDetails { get; set; }

        /// <summary>
        /// Time taken to download update (milliseconds).
        /// </summary>
        public long? DownloadTimeMs { get; set; }

        /// <summary>
        /// Time taken to apply update (milliseconds).
        /// </summary>
        public long? ApplyTimeMs { get; set; }

        /// <summary>
        /// Number of times this update has been deferred.
        /// </summary>
        public int? DeferralCount { get; set; }

        /// <summary>
        /// Anonymized machine identifier (hashed).
        /// </summary>
        public string MachineId { get; set; }
    }
}
