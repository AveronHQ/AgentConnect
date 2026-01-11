using System;

namespace AgentConnect.Updates.Models
{
    /// <summary>
    /// Tracks deferral state for a specific version.
    /// Persisted to user's local app data.
    /// </summary>
    public class DeferralState
    {
        /// <summary>
        /// The version this deferral state applies to.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Number of times the user has deferred this update.
        /// </summary>
        public int DeferralCount { get; set; }

        /// <summary>
        /// When the user was first prompted about this update.
        /// Used to calculate time-based force.
        /// </summary>
        public DateTime FirstPromptTime { get; set; }

        /// <summary>
        /// When the user last deferred this update.
        /// </summary>
        public DateTime? LastDeferralTime { get; set; }

        /// <summary>
        /// The update is deferred until this time.
        /// </summary>
        public DateTime? DeferUntil { get; set; }
    }
}
