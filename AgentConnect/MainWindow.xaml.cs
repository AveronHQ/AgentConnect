using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
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
        private bool _isDarkTheme = false;

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

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme(_isDarkTheme);
            UpdateToggleButtonVisuals();
        }

        private void ApplyTheme(bool isDark)
        {
            var themeUri = isDark
                ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
                : new Uri("Themes/LightTheme.xaml", UriKind.Relative);

            var newTheme = new ResourceDictionary { Source = themeUri };

            // Replace the theme dictionary
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(newTheme);
        }

        private void UpdateToggleButtonVisuals()
        {
            // Find the template elements
            var button = ThemeToggleButton;
            if (button.Template.FindName("SunIcon", button) is Path sunIcon &&
                button.Template.FindName("MoonIcon", button) is Path moonIcon &&
                button.Template.FindName("ThemeText", button) is TextBlock themeText)
            {
                if (_isDarkTheme)
                {
                    sunIcon.Visibility = Visibility.Collapsed;
                    moonIcon.Visibility = Visibility.Visible;
                    themeText.Text = "Dark";
                }
                else
                {
                    sunIcon.Visibility = Visibility.Visible;
                    moonIcon.Visibility = Visibility.Collapsed;
                    themeText.Text = "Light";
                }
            }
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
