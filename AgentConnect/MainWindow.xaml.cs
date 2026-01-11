using System.Windows;

#if DEBUG
using AgentConnect.DevTools;
using AgentConnect.Updates.Models;
using AgentConnect.Updates.UI;
#endif

namespace AgentConnect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            // Add debug overlay - auto-attaches to this window's events
            RootGrid.Children.Add(new UIMetricsOverlay());

            // Show debug button for testing update prompt
            DebugUpdateButton.Visibility = Visibility.Visible;
#endif
        }

#if DEBUG
        private void DebugUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var mockUpdateInfo = new ExtendedUpdateInfo
            {
                TargetVersion = "1.1.0.0",
                Type = UpdateType.Prompted,
                ReleaseNotes = "• New feature: Auto-update system\n• Improved performance\n• Bug fixes and stability improvements",
                MaxDeferrals = 3,
                IsCritical = false
            };

            var mockUpdateService = new MockUpdateService();
            var updateWindow = new UpdatePromptWindow(mockUpdateInfo, mockUpdateService);
            updateWindow.Owner = this;
            updateWindow.ShowDialog();
        }
#endif
    }
}
