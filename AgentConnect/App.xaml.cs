using System;
using System.Windows;
using AgentConnect.Services;
using AgentConnect.Updates.Services;

namespace AgentConnect
{
    public partial class App : Application
    {
        private IUpdateScheduler _updateScheduler;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            UpdateLogger.Log("App", "========================================");
            UpdateLogger.Log("App", "=== APPLICATION STARTUP ===");
            UpdateLogger.Log("App", "========================================");
            UpdateLogger.Log("App", $"Log file: {UpdateLogger.LogPath}");
            UpdateLogger.Log("App", $"Args: {string.Join(", ", e.Args)}");

            UpdateLogger.Log("App", "Creating SplashScreen...");
            var splashScreen = new SplashScreen();
            splashScreen.Show();
            UpdateLogger.Log("App", "SplashScreen shown");

            UpdateLogger.Log("App", "Initializing update system...");
            InitializeUpdateSystem();
            UpdateLogger.Log("App", "Update system initialized");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            UpdateLogger.Log("App", "=== APPLICATION EXIT ===");
            _updateScheduler?.Dispose();
            base.OnExit(e);
        }

        private void InitializeUpdateSystem()
        {
            UpdateLogger.Log("App", "=== InitializeUpdateSystem START ===");

            try
            {
                UpdateLogger.Log("App", "Creating UpdateManifestService...");
                var manifestService = new UpdateManifestService();

                UpdateLogger.Log("App", "Creating DeferralService...");
                var deferralService = new DeferralService();

                UpdateLogger.Log("App", "Creating UpdateTelemetryService...");
                var telemetryService = new UpdateTelemetryService(enabled: false);

                UpdateLogger.Log("App", "Creating UpdateService...");
                var updateService = new UpdateService(
                    manifestService,
                    deferralService,
                    telemetryService);

                UpdateLogger.Log("App", $"UpdateService.IsInstalled: {updateService.IsInstalled}");
                UpdateLogger.Log("App", $"UpdateService.CurrentVersion: {updateService.CurrentVersion}");

                if (updateService.IsInstalled)
                {
                    UpdateLogger.Log("App", "App IS installed - creating UpdateScheduler...");
                    _updateScheduler = new UpdateScheduler(updateService, this.Dispatcher);

                    UpdateLogger.Log("App", "Subscribing to UpdateAvailable event...");
                    _updateScheduler.UpdateAvailable += OnUpdateAvailable;

                    UpdateLogger.Log("App", "Starting scheduler...");
                    _updateScheduler.Start();
                    UpdateLogger.Log("App", "Scheduler started");
                }
                else
                {
                    UpdateLogger.Log("App", "App is NOT installed - skipping update scheduler");
                }
            }
            catch (Exception ex)
            {
                UpdateLogger.LogException("App", ex);
            }

            UpdateLogger.Log("App", "=== InitializeUpdateSystem END ===");
        }

        private void OnUpdateAvailable(object sender, Updates.Models.ExtendedUpdateInfo updateInfo)
        {
            UpdateLogger.Log("App", "=== OnUpdateAvailable EVENT RECEIVED ===");
            UpdateLogger.Log("App", $"Update type: {updateInfo.Type}");
            UpdateLogger.Log("App", $"Target version: {updateInfo.TargetVersion}");

            Dispatcher.Invoke(() =>
            {
                UpdateLogger.Log("App", "Inside Dispatcher.Invoke");

                var updateService = ((UpdateScheduler)sender).UpdateService;

                // Show the update button in MainWindow header if available
                if (MainWindow is MainWindow mainWin)
                {
                    UpdateLogger.Log("App", "Showing update button in MainWindow header");
                    mainWin.ShowUpdateAvailable(updateInfo, updateService);
                }

                if (updateInfo.Type != Updates.Models.UpdateType.Silent)
                {
                    UpdateLogger.Log("App", "Creating UpdatePromptWindow...");
                    var promptWindow = new Updates.UI.UpdatePromptWindow(updateInfo, updateService);

                    // Don't set Owner - keep the window independent so it stays open
                    // when splash closes and main window opens
                    UpdateLogger.Log("App", "Showing UpdatePromptWindow (independent, topmost)...");
                    promptWindow.Show();
                    promptWindow.Activate();
                    UpdateLogger.Log("App", "UpdatePromptWindow shown");
                }
                else
                {
                    UpdateLogger.Log("App", "Silent update - not showing prompt");
                }
            });
        }
    }
}
