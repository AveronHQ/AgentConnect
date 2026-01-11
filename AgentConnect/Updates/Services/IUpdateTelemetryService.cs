using System.Threading.Tasks;
using AgentConnect.Updates.Models;

namespace AgentConnect.Updates.Services
{
    /// <summary>
    /// Service interface for tracking update telemetry.
    /// Note: Currently disabled, interfaces defined for future use.
    /// </summary>
    public interface IUpdateTelemetryService
    {
        /// <summary>
        /// Track a telemetry event.
        /// </summary>
        /// <param name="telemetryEvent">The event to track.</param>
        Task TrackEventAsync(UpdateTelemetryEvent telemetryEvent);

        /// <summary>
        /// Flush any queued events.
        /// </summary>
        Task FlushAsync();
    }
}
