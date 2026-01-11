using System.Collections.Generic;

namespace AgentConnect.Updates.Models
{
    /// <summary>
    /// Server-side manifest that accompanies GitHub releases.
    /// Stored as update-manifest.json in release assets.
    /// </summary>
    public class UpdateManifest
    {
        /// <summary>
        /// The version this manifest describes.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The release channel (e.g., "stable", "beta").
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// The type of update (determines deferral behavior).
        /// </summary>
        public UpdateType Type { get; set; }

        /// <summary>
        /// Short release notes for display in update prompt.
        /// </summary>
        public string ReleaseNotes { get; set; }

        /// <summary>
        /// URL to full release notes page.
        /// </summary>
        public string ReleaseNotesUrl { get; set; }

        /// <summary>
        /// Maximum number of times the update can be deferred.
        /// Default: 3
        /// </summary>
        public int MaxDeferrals { get; set; } = 3;

        /// <summary>
        /// Number of minutes after first prompt until update becomes forced.
        /// Default: 10080 (7 days)
        /// </summary>
        public int MinutesUntilForced { get; set; } = 10080;

        /// <summary>
        /// Minimum version required to update to this version.
        /// If user's version is older, they may need intermediate updates.
        /// </summary>
        public string MinimumVersion { get; set; }

        /// <summary>
        /// List of versions that are deprecated and should update immediately.
        /// </summary>
        public List<string> DeprecatedVersions { get; set; }
    }
}
