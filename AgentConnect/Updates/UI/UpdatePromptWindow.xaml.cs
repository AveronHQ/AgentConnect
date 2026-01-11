using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using AgentConnect.Updates.Models;
using AgentConnect.Updates.Services;

namespace AgentConnect.Updates.UI
{
    /// <summary>
    /// Update prompt dialog that shows when a prompted, forced, or security update is available.
    /// </summary>
    public partial class UpdatePromptWindow : Window, INotifyPropertyChanged
    {
        private readonly ExtendedUpdateInfo _updateInfo;
        private readonly IUpdateService _updateService;
        private int _downloadProgress;
        private bool _isDownloading;

        public string CurrentVersion { get; }
        public string NewVersion { get; }
        public string ReleaseNotes { get; }
        public bool CanDefer { get; }
        public int DeferralsRemaining { get; }
        public bool ShowDeferralInfo => CanDefer && DeferralsRemaining > 0;

        public string UpdateTypeDescription
        {
            get
            {
                if (_updateInfo == null)
                    return "A new update is ready to install";

                switch (_updateInfo.Type)
                {
                    case UpdateType.SecurityHotfix:
                        return "Security update - immediate installation required";
                    case UpdateType.Forced:
                        return "Important update - installation required";
                    case UpdateType.Prompted:
                        return "A new update is ready to install";
                    default:
                        return "Update available";
                }
            }
        }

        public int DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                OnPropertyChanged();
            }
        }

        public bool UserAccepted { get; private set; }

#if DEBUG
        /// <summary>
        /// Debug-only constructor for UI preview during development.
        /// </summary>
        public UpdatePromptWindow()
        {
            InitializeComponent();
            DataContext = this;

            _updateInfo = null;
            _updateService = null;

            CurrentVersion = "1.0.0.0";
            NewVersion = "1.1.0.0";
            ReleaseNotes = "• New feature: Auto-update system\n• Improved performance\n• Bug fixes and stability improvements";
            CanDefer = true;
            DeferralsRemaining = 3;
        }
#endif

        public UpdatePromptWindow(ExtendedUpdateInfo updateInfo, IUpdateService updateService)
        {
            InitializeComponent();
            DataContext = this;

            _updateInfo = updateInfo;
            _updateService = updateService;

            CurrentVersion = updateService.CurrentVersion;
            NewVersion = updateInfo.TargetVersion;
            ReleaseNotes = updateInfo.ReleaseNotes ?? "No release notes available.";
            CanDefer = updateService.CanDeferUpdate(updateInfo);
            DeferralsRemaining = Math.Max(0, updateInfo.MaxDeferrals - GetCurrentDeferralCount());

            // For forced/security updates, prevent closing without installing
            if (updateInfo.IsCritical)
            {
                this.Closing += OnClosing;
            }

            // Subscribe to download progress
            _updateService.DownloadProgressChanged += OnDownloadProgressChanged;
        }

        private int GetCurrentDeferralCount()
        {
            // This is a simplified implementation
            // In a full implementation, we'd query the deferral service
            return 0;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            // Prevent closing critical update dialogs without installing
            if (_updateInfo.IsCritical && !UserAccepted && !_isDownloading)
            {
                e.Cancel = true;
                MessageBox.Show(
                    "This update is required and cannot be skipped. Please click 'Install Now' to continue.",
                    "Update Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void OnDownloadProgressChanged(object sender, int progress)
        {
            Dispatcher.Invoke(() =>
            {
                DownloadProgress = progress;
            });
        }

        private async void DeferButton_Click(object sender, RoutedEventArgs e)
        {
            var deferred = await _updateService.DeferUpdateAsync(_updateInfo);
            if (deferred)
            {
                UserAccepted = false;
                _updateService.DownloadProgressChanged -= OnDownloadProgressChanged;
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    "This update can no longer be deferred. Maximum deferrals reached or time limit exceeded.",
                    "Update Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Update UI to reflect that deferral is no longer possible
                DeferButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            _isDownloading = true;
            InstallButton.IsEnabled = false;
            DeferButton.IsEnabled = false;
            InstallButton.Content = "Downloading...";

            try
            {
                var progress = new Progress<int>(p =>
                {
                    DownloadProgress = p;
                    InstallButton.Content = $"Downloading... {p}%";
                });

                await _updateService.DownloadUpdateAsync(_updateInfo, progress);

                InstallButton.Content = "Installing...";
                UserAccepted = true;

                // Unsubscribe before restart
                _updateService.DownloadProgressChanged -= OnDownloadProgressChanged;

                _updateService.ApplyUpdateAndRestart(_updateInfo);
            }
            catch (Exception ex)
            {
                _isDownloading = false;
                MessageBox.Show(
                    $"Failed to download update: {ex.Message}\n\nPlease try again later.",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                InstallButton.IsEnabled = true;
                DeferButton.IsEnabled = CanDefer;
                InstallButton.Content = "Install Now";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
