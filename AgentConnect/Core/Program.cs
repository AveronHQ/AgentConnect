using System;
using System.Windows;
using Velopack;

namespace AgentConnect.Core
{
    /// <summary>
    /// Application entry point with Velopack bootstrap for auto-updates.
    /// This must run before any WPF code to handle install/uninstall/update hooks.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Initialize Velopack as early as possible
            // This handles install/uninstall/update hooks before the app starts
            VelopackApp.Build()
                .WithFirstRun(OnFirstRun)
                .WithBeforeUpdateFastCallback(OnBeforeUpdate)
                .WithAfterUpdateFastCallback(OnAfterUpdate)
                .WithBeforeUninstallFastCallback(OnBeforeUninstall)
                .Run();

            // Start WPF application
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        /// <summary>
        /// Called on first run after installation.
        /// Use for initializing settings, creating directories, etc.
        /// </summary>
        private static void OnFirstRun(SemanticVersion version)
        {
            // Initialize any first-run settings here
            // Example: Create app data directories, set default preferences
        }

        /// <summary>
        /// Called just before an update is applied.
        /// Use for cleanup, saving state, etc.
        /// </summary>
        private static void OnBeforeUpdate(SemanticVersion version)
        {
            // Perform any cleanup before update
            // Example: Close file handles, save pending data
        }

        /// <summary>
        /// Called immediately after an update is applied.
        /// Use for migration, cleanup of old files, etc.
        /// </summary>
        private static void OnAfterUpdate(SemanticVersion version)
        {
            // Perform post-update tasks
            // Example: Migrate settings, clean up old cache
        }

        /// <summary>
        /// Called before the application is uninstalled.
        /// Use for cleanup of user data if appropriate.
        /// </summary>
        private static void OnBeforeUninstall(SemanticVersion version)
        {
            // Clean up on uninstall
            // Note: Be careful not to delete user data they might want to keep
        }
    }
}
