namespace AgentConnect.Updates.Configuration
{
    /// <summary>
    /// Configuration constants for the auto-update system.
    /// </summary>
    public static class UpdateConstants
    {
        // Application Identity
        public const string ApplicationName = "AgentConnect";
        public const string Publisher = "AveronHQ";
        public const string ProductCode = "com.averonhq.agentconnect";

        // GitHub Repository
        public const string GitHubOwner = "AveronHQ";
        public const string GitHubRepo = "AgentConnect";
        public const string GitHubRepoUrl = "https://github.com/AveronHQ/AgentConnect";

        // Update Settings
        public const string DefaultChannel = "stable";
        public const int UpdateCheckIntervalHours = 24;
        public const int MaxDeferralsDefault = 3;
        public const int DeferralHours = 24;
        public const int MinutesUntilForcedDefault = 10080; // 7 days

        // Manifest file name in GitHub release assets
        public const string ManifestFileName = "update-manifest.json";

        // Local storage paths (relative to LocalApplicationData)
        public const string AppDataFolderName = "AgentConnect";
        public const string UpdatesFolderName = "Updates";
        public const string DeferralFileName = "deferrals.json";

        // Telemetry (disabled for now - set endpoint when ready)
        public const string TelemetryEndpoint = null;
    }
}
