using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AgentConnect.Services;

namespace AgentConnect
{
    public partial class SplashScreen : Window
    {
        private readonly NetworkConnectivityService _connectivityService;

        public SplashScreen()
        {
            InitializeComponent();
            _connectivityService = new NetworkConnectivityService();

            // Set version text
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";

            Loaded += SplashScreen_Loaded;
        }

        private async void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            // Check connectivity (fast - max 1 second timeout)
            var result = await _connectivityService.CheckConnectivityAsync();

            // Update UI
            UpdateIndicator(InternetIndicator, InternetStatusText, result.HasInternet, "Connected", "Offline");
            UpdateIndicator(IntranetIndicator, IntranetStatusText, result.HasIntranet, "Connected", "Unavailable");

            LoadingText.Text = "Ready";

            // Wait 3 seconds then proceed
            await Task.Delay(3000);

            // Go to main window
            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            Close();
        }

        private void UpdateIndicator(System.Windows.Shapes.Ellipse indicator,
            System.Windows.Controls.TextBlock statusText, bool isConnected, string connectedText, string disconnectedText)
        {
            if (isConnected)
            {
                indicator.Fill = new SolidColorBrush(Color.FromRgb(0x34, 0xA8, 0x53)); // Green
                statusText.Text = connectedText;
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xA8, 0x53));
            }
            else
            {
                indicator.Fill = new SolidColorBrush(Color.FromRgb(0xF5, 0x7C, 0x00)); // Amber
                statusText.Text = disconnectedText;
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0x7C, 0x00));
            }
        }
    }
}
