using System.Windows;
using AgentConnect.Updates.UI;

#if DEBUG
using AgentConnect.DevTools;
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

        // Handler must exist for XAML binding even in Release (button is hidden)
        private void DebugUpdateButton_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            var updateWindow = new UpdatePromptWindow();
            updateWindow.Owner = this;
            updateWindow.ShowDialog();
#endif
        }
    }
}
