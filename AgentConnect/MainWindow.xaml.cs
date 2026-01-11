using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using AgentConnect.Updates.Models;
using AgentConnect.Updates.Services;
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
        private ExtendedUpdateInfo _pendingUpdate;
        private IUpdateService _updateService;

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

        #region Update Management

        public void ShowUpdateAvailable(ExtendedUpdateInfo updateInfo, IUpdateService updateService)
        {
            _pendingUpdate = updateInfo;
            _updateService = updateService;
            UpdateAvailableButton.Visibility = Visibility.Visible;
        }

        public void HideUpdateAvailable()
        {
            _pendingUpdate = null;
            _updateService = null;
            UpdateAvailableButton.Visibility = Visibility.Collapsed;
        }

        private void UpdateAvailableButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingUpdate != null && _updateService != null)
            {
                var promptWindow = new UpdatePromptWindow(_pendingUpdate, _updateService);
                promptWindow.Show();
                promptWindow.Activate();
            }
        }

        #endregion

        #region Theme Management

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

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(newTheme);
        }

        private void UpdateToggleButtonVisuals()
        {
            if (SunIcon != null && MoonIcon != null)
            {
                if (_isDarkTheme)
                {
                    SunIcon.Visibility = Visibility.Collapsed;
                    MoonIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    SunIcon.Visibility = Visibility.Visible;
                    MoonIcon.Visibility = Visibility.Collapsed;
                }
            }
        }

        #endregion

        #region Navigation

        private void HamburgerMenuButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open side navigation drawer
            ShowPlaceholderMessage("Menu", "Side navigation drawer will open here");
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to home/dashboard
            ShowPlaceholderMessage("Home", "Navigating to Dashboard");
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate back in history
            ShowPlaceholderMessage("Navigation", "Going back in history");
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate forward in history
            ShowPlaceholderMessage("Navigation", "Going forward in history");
        }

        #endregion

        #region Search & Commands

        private void CommandPaletteButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open command palette modal
            ShowPlaceholderMessage("Command Palette", "Press Ctrl+K to open the command palette");
        }

        #endregion

        #region Quick Actions

        private void QuickCreateButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open quick create menu/dialog
            ShowPlaceholderMessage("Create", "Quick create menu will open here");
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open favorites panel
            ShowPlaceholderMessage("Favorites", "Your bookmarked items will appear here");
        }

        private void RecentButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open recent items panel
            ShowPlaceholderMessage("Recent", "Recently accessed items will appear here");
        }

        private void SyncStatusButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show sync status details
            ShowPlaceholderMessage("Sync Status", "All changes are saved and synced");
        }

        #endregion

        #region Communication

        private void MessagesButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open messages panel
            ShowPlaceholderMessage("Messages", "You have 3 unread messages");
        }

        private void NotificationsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open notifications panel
            ShowPlaceholderMessage("Notifications", "You have 5 new notifications");
        }

        private void ActivityButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open activity feed
            ShowPlaceholderMessage("Activity", "Recent activity feed will appear here");
        }

        #endregion

        #region Help & Settings

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open help center
            ShowPlaceholderMessage("Help", "Help & Support center will open here");
        }

        private void ShortcutsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show keyboard shortcuts dialog
            ShowPlaceholderMessage("Keyboard Shortcuts", "Keyboard shortcuts reference will appear here");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open settings
            ShowPlaceholderMessage("Settings", "Application settings will open here");
        }

        #endregion

        #region Workspace & Profile

        private void WorkspaceSwitcher_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open workspace switcher dropdown
            ShowPlaceholderMessage("Workspaces", "Switch between workspaces here");
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open profile dropdown menu
            ShowPlaceholderMessage("Profile", "Profile menu with account settings, logout, etc.");
        }

        #endregion

        #region Debug

        private void DebugUpdateButton_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            var updateWindow = new UpdatePromptWindow();
            updateWindow.Show();
#endif
        }

        #endregion

        #region Helpers

        private void ShowPlaceholderMessage(string title, string message)
        {
            // Placeholder for showing messages - in production this would open actual UI
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}
