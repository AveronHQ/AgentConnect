using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AgentConnect.Updates.Configuration;
using AgentConnect.Updates.Models;
using Newtonsoft.Json;

namespace AgentConnect.Updates.Services
{
    /// <summary>
    /// Service for fetching update manifests from GitHub release assets.
    /// </summary>
    public class UpdateManifestService : IUpdateManifestService
    {
        private readonly HttpClient _httpClient;

        public UpdateManifestService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                $"{UpdateConstants.ApplicationName}/{GetType().Assembly.GetName().Version}");
        }

        public async Task<UpdateManifest> GetManifestAsync(string version, CancellationToken cancellationToken = default)
        {
            try
            {
                // GitHub release asset URL format:
                // https://github.com/{owner}/{repo}/releases/download/v{version}/{filename}
                var manifestUrl = $"{UpdateConstants.GitHubRepoUrl}/releases/download/v{version}/{UpdateConstants.ManifestFileName}";

                var response = await _httpClient.GetAsync(manifestUrl, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // Manifest might not exist for older releases
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UpdateManifest>(json);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                // Network error or timeout - return null, update can still proceed with defaults
                System.Diagnostics.Debug.WriteLine($"Failed to fetch manifest: {ex.Message}");
                return null;
            }
        }
    }
}
