using System;
using System.Windows;
using AgentConnect.Updates.Services;

namespace AgentConnect
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IUpdateScheduler _updateScheduler;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Show splash screen first (it will show MainWindow after connectivity checks)
            var splashScreen = new SplashScreen();
            splashScreen.Show();

            // Initialize and start update scheduler
            InitializeUpdateSystem();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up update scheduler
            _updateScheduler?.Dispose();
            base.OnExit(e);
        }

        private void InitializeUpdateSystem()
        {
            try
            {
                // Create update services
                var manifestService = new UpdateManifestService();
                var deferralService = new DeferralService();
                var telemetryService = new UpdateTelemetryService(enabled: false); // Telemetry disabled for now

                var updateService = new UpdateService(
                    manifestService,
                    deferralService,
                    telemetryService);

                // Only start scheduler if running as installed app
                if (updateService.IsInstalled)
                {
                    _updateScheduler = new UpdateScheduler(updateService, this.Dispatcher);
                    _updateScheduler.UpdateAvailable += OnUpdateAvailable;
                    _updateScheduler.Start();
                }
            }
            catch (Exception ex)
            {
                // Don't crash the app if update system fails to initialize
                System.Diagnostics.Debug.WriteLine($"Update system initialization failed: {ex.Message}");
            }
        }

        private void OnUpdateAvailable(object sender, Updates.Models.ExtendedUpdateInfo updateInfo)
        {
            // Handle update available event on UI thread
            Dispatcher.Invoke(() =>
            {
                // For prompted/forced/security updates, show the prompt window
                if (updateInfo.Type != Updates.Models.UpdateType.Silent)
                {
                    var updateService = ((UpdateScheduler)sender).UpdateService;
                    var promptWindow = new Updates.UI.UpdatePromptWindow(updateInfo, updateService);
                    promptWindow.Owner = MainWindow;
                    promptWindow.ShowDialog();
                }
            });
        }
    }
}
