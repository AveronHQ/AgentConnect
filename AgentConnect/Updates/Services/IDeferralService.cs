using System.Threading.Tasks;
using AgentConnect.Updates.Models;

namespace AgentConnect.Updates.Services
{
    /// <summary>
    /// Service interface for managing update deferrals.
    /// </summary>
    public interface IDeferralService
    {
        /// <summary>
        /// Get the current deferral state for a version.
        /// </summary>
        /// <param name="version">The version to check.</param>
        /// <returns>Deferral state if exists, null otherwise.</returns>
        Task<DeferralState> GetDeferralStateAsync(string version);

        /// <summary>
        /// Defer an update.
        /// </summary>
        /// <param name="version">The version being deferred.</param>
        /// <param name="maxDeferrals">Maximum allowed deferrals.</param>
        /// <returns>True if deferral was successful.</returns>
        Task<bool> DeferUpdateAsync(string version, int maxDeferrals);

        /// <summary>
        /// Check if an update can be deferred.
        /// </summary>
        /// <param name="version">The version to check.</param>
        /// <param name="maxDeferrals">Maximum allowed deferrals.</param>
        /// <param name="minutesUntilForced">Minutes until update becomes forced.</param>
        /// <returns>True if deferral is allowed.</returns>
        Task<bool> CanDeferAsync(string version, int maxDeferrals, int minutesUntilForced);

        /// <summary>
        /// Clear deferral state for a version (e.g., after update is applied).
        /// </summary>
        /// <param name="version">The version to clear.</param>
        Task ClearDeferralAsync(string version);
    }
}
