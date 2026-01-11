using System.Threading.Tasks;
using AgentConnect.Updates.Models;

namespace AgentConnect.Updates.Services
{
    /// <summary>
    /// Stub telemetry service - tracking is disabled until a backend endpoint is configured.
    /// The interface and models are ready for future implementation.
    /// </summary>
    public class UpdateTelemetryService : IUpdateTelemetryService
    {
        private readonly bool _enabled;

        public UpdateTelemetryService(bool enabled = false)
        {
            _enabled = enabled;
        }

        public Task TrackEventAsync(UpdateTelemetryEvent telemetryEvent)
        {
            // Telemetry is disabled - no-op
            // When ready to enable:
            // 1. Set UpdateConstants.TelemetryEndpoint to your endpoint URL
            // 2. Implement actual HTTP POST logic here
            // 3. Queue events and batch send
            return Task.CompletedTask;
        }

        public Task FlushAsync()
        {
            // Telemetry is disabled - no-op
            return Task.CompletedTask;
        }
    }
}
