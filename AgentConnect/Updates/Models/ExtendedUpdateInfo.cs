using System;
using Velopack;

namespace AgentConnect.Updates.Models
{
    /// <summary>
    /// Extended update information that wraps Velopack's UpdateInfo
    /// with additional metadata from the server manifest.
    /// </summary>
    public class ExtendedUpdateInfo
    {
        /// <summary>
        /// The underlying Velopack update information.
        /// </summary>
        public UpdateInfo VelopackUpdateInfo { get; set; }

        /// <summary>
        /// The type of update determining how it should be applied.
        /// </summary>
        public UpdateType Type { get; set; }

        /// <summary>
        /// Release notes for display to user.
        /// </summary>
        public string ReleaseNotes { get; set; }

        /// <summary>
        /// URL to full release notes page.
        /// </summary>
        public string ReleaseNotesUrl { get; set; }

        /// <summary>
        /// If set, the update cannot be applied until after this time (deferral active).
        /// </summary>
        public DateTime? DeferUntil { get; set; }

        /// <summary>
        /// Maximum number of times this update can be deferred.
        /// </summary>
        public int MaxDeferrals { get; set; }

        /// <summary>
        /// Number of minutes after first prompt until update becomes forced.
        /// </summary>
        public int MinutesUntilForced { get; set; }

        /// <summary>
        /// Whether this update requires immediate action (Forced or SecurityHotfix).
        /// </summary>
        public bool IsCritical => Type == UpdateType.Forced || Type == UpdateType.SecurityHotfix;

        /// <summary>
        /// The target version string.
        /// </summary>
        public string TargetVersion => VelopackUpdateInfo?.TargetFullRelease?.Version?.ToString();
    }
}
