using System.Windows;

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
#endif
        }
    }
}
