using System;
using AgentConnect.Updates.Models;

namespace AgentConnect.Updates.Services
{
    /// <summary>
    /// Service interface for scheduling automatic update checks.
    /// </summary>
    public interface IUpdateScheduler : IDisposable
    {
        /// <summary>
        /// The underlying update service.
        /// </summary>
        IUpdateService UpdateService { get; }

        /// <summary>
        /// Event raised when an update check completes.
        /// </summary>
        event EventHandler<ExtendedUpdateInfo> UpdateAvailable;

        /// <summary>
        /// Start the scheduler (checks immediately and on interval).
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the scheduler.
        /// </summary>
        void Stop();

        /// <summary>
        /// Trigger an immediate update check.
        /// </summary>
        System.Threading.Tasks.Task CheckNowAsync();
    }
}
