using System.Threading;
using System.Threading.Tasks;
using AgentConnect.Updates.Models;

namespace AgentConnect.Updates.Services
{
    /// <summary>
    /// Service interface for fetching update manifests from GitHub releases.
    /// </summary>
    public interface IUpdateManifestService
    {
        /// <summary>
        /// Get the manifest for a specific version from GitHub release assets.
        /// </summary>
        /// <param name="version">The version to get manifest for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The manifest if found, null otherwise.</returns>
        Task<UpdateManifest> GetManifestAsync(string version, CancellationToken cancellationToken = default);
    }
}
