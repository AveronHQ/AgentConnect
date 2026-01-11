namespace AgentConnect.Updates.Models
{
    /// <summary>
    /// Defines the type of update which determines how it is applied and whether it can be deferred.
    /// </summary>
    public enum UpdateType
    {
        /// <summary>
        /// Downloads and applies silently in background without user interaction.
        /// Applied automatically on next app restart.
        /// </summary>
        Silent = 0,

        /// <summary>
        /// Prompts user to install; can be deferred up to server-configured limit.
        /// Default update type for regular releases.
        /// </summary>
        Prompted = 1,

        /// <summary>
        /// Prompts user but cannot be deferred; must install before continuing.
        /// Use for important updates that should not be skipped.
        /// </summary>
        Forced = 2,

        /// <summary>
        /// Security hotfix; cannot be deferred; applied immediately.
        /// Use for critical security vulnerabilities.
        /// </summary>
        SecurityHotfix = 3
    }
}
