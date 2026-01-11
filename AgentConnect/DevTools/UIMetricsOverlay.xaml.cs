using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Forms;
using UserControl = System.Windows.Controls.UserControl;

namespace AgentConnect.DevTools
{
    /// <summary>
    /// A self-contained debug overlay that displays UI metrics for any window.
    /// Simply drop this UserControl into any window and it will auto-attach to parent window events.
    /// </summary>
    public partial class UIMetricsOverlay : UserControl
    {
        private Window _parentWindow;
        private bool _isDragging;
        private Point _dragStartPoint;
        private Point _dragStartOffset;
        private bool _isDetailsOpen;

        public UIMetricsOverlay()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.SizeChanged += ParentWindow_SizeChanged;
                _parentWindow.LocationChanged += ParentWindow_LocationChanged;
                _parentWindow.StateChanged += ParentWindow_StateChanged;
                UpdateAllMetrics();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
            {
                _parentWindow.SizeChanged -= ParentWindow_SizeChanged;
                _parentWindow.LocationChanged -= ParentWindow_LocationChanged;
                _parentWindow.StateChanged -= ParentWindow_StateChanged;
                _parentWindow = null;
            }
        }

        private void ParentWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateAllMetrics();
            ConstrainButtonPosition();
        }

        private void ParentWindow_LocationChanged(object sender, EventArgs e)
        {
            UpdateAllMetrics();
        }

        private void ParentWindow_StateChanged(object sender, EventArgs e)
        {
            UpdateAllMetrics();
        }

        private void UpdateAllMetrics()
        {
            if (_parentWindow == null) return;

            UpdateWindowMetrics();
            UpdateDpiMetrics();
            UpdateScreenMetrics();
            UpdateRenderingMetrics();
        }

        private void UpdateWindowMetrics()
        {
            if (_parentWindow == null) return;

            int actualWidth = (int)_parentWindow.ActualWidth;
            int actualHeight = (int)_parentWindow.ActualHeight;

            // Main button display
            DimensionsText.Text = $"{actualWidth} × {actualHeight}";

            // Window section
            string xamlWidth = double.IsNaN(_parentWindow.Width) ? "Auto" : $"{(int)_parentWindow.Width}";
            string xamlHeight = double.IsNaN(_parentWindow.Height) ? "Auto" : $"{(int)_parentWindow.Height}";
            DetailSize.Text = $"{xamlWidth} × {xamlHeight}";
            DetailActualSize.Text = $"{actualWidth} × {actualHeight}";

            // Client area (content area without chrome)
            var content = _parentWindow.Content as FrameworkElement;
            if (content != null)
            {
                DetailClientArea.Text = $"{(int)content.ActualWidth} × {(int)content.ActualHeight}";
            }

            // Position
            DetailPosition.Text = $"{(int)_parentWindow.Left}, {(int)_parentWindow.Top}";

            // Window state
            DetailWindowState.Text = _parentWindow.WindowState.ToString();

            // Topmost
            DetailTopmost.Text = _parentWindow.Topmost ? "Yes" : "No";
        }

        private void UpdateDpiMetrics()
        {
            if (_parentWindow == null) return;

            var source = PresentationSource.FromVisual(_parentWindow);
            if (source?.CompositionTarget != null)
            {
                var dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                var dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
                var scaleX = source.CompositionTarget.TransformToDevice.M11;
                var scaleY = source.CompositionTarget.TransformToDevice.M22;

                DetailDpi.Text = $"{(int)dpiX} × {(int)dpiY}";
                DetailScaleFactor.Text = $"{(int)(scaleX * 100)}%";
                DetailPixelsPerDip.Text = $"{scaleX:F2}";

                // Physical pixel size
                int physicalWidth = (int)(_parentWindow.ActualWidth * scaleX);
                int physicalHeight = (int)(_parentWindow.ActualHeight * scaleY);
                DetailPhysicalSize.Text = $"{physicalWidth} × {physicalHeight} px";
            }
        }

        private void UpdateScreenMetrics()
        {
            if (_parentWindow == null) return;

            // Get the screen the window is on
            var windowInteropHelper = new WindowInteropHelper(_parentWindow);
            var screen = Screen.FromHandle(windowInteropHelper.Handle);
            var allScreens = Screen.AllScreens;

            // Screen resolution
            DetailScreenRes.Text = $"{screen.Bounds.Width} × {screen.Bounds.Height}";

            // Work area (excluding taskbar)
            DetailWorkArea.Text = $"{screen.WorkingArea.Width} × {screen.WorkingArea.Height}";

            // Primary screen
            DetailPrimaryScreen.Text = screen.Primary ? "Yes" : "No";

            // Monitor number
            int monitorIndex = Array.IndexOf(allScreens, screen) + 1;
            DetailMonitorNumber.Text = $"{monitorIndex} of {allScreens.Length}";
        }

        private void UpdateRenderingMetrics()
        {
            // Render tier
            int renderTier = (RenderCapability.Tier >> 16);
            string tierDescription;
            switch (renderTier)
            {
                case 0:
                    tierDescription = "Tier 0 (SW)";
                    break;
                case 1:
                    tierDescription = "Tier 1 (Partial HW)";
                    break;
                case 2:
                    tierDescription = "Tier 2 (Full HW)";
                    break;
                default:
                    tierDescription = $"Tier {renderTier}";
                    break;
            }
            DetailRenderTier.Text = tierDescription;

            // GPU acceleration
            DetailGpuAccel.Text = renderTier >= 1 ? "Yes" : "No";
        }

        private void FloatingButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            _dragStartOffset = new Point(FloatingButtonTransform.X, FloatingButtonTransform.Y);
            FloatingButton.CaptureMouse();
            e.Handled = true;
        }

        private void FloatingButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                FloatingButton.ReleaseMouseCapture();

                Point endPoint = e.GetPosition(this);
                double distance = Math.Sqrt(
                    Math.Pow(endPoint.X - _dragStartPoint.X, 2) +
                    Math.Pow(endPoint.Y - _dragStartPoint.Y, 2));

                // Only toggle if it was a click (not a drag)
                if (distance < 5)
                {
                    ToggleDetails();
                }

                _isDragging = false;
                e.Handled = true;
            }
        }

        private void FloatingButton_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && _parentWindow != null)
            {
                Point currentPoint = e.GetPosition(this);
                double deltaX = currentPoint.X - _dragStartPoint.X;
                double deltaY = currentPoint.Y - _dragStartPoint.Y;

                double newX = _dragStartOffset.X + deltaX;
                double newY = _dragStartOffset.Y + deltaY;

                // Constrain to window bounds
                double maxX = _parentWindow.ActualWidth - FloatingButton.ActualWidth - FloatingButton.Margin.Left;
                double maxY = _parentWindow.ActualHeight - FloatingButton.ActualHeight - FloatingButton.Margin.Top;
                double minX = -FloatingButton.Margin.Left;
                double minY = -FloatingButton.Margin.Top;

                newX = Math.Max(minX, Math.Min(maxX, newX));
                newY = Math.Max(minY, Math.Min(maxY, newY));

                FloatingButtonTransform.X = newX;
                FloatingButtonTransform.Y = newY;
                DetailsPopupTransform.X = newX;
                DetailsPopupTransform.Y = newY;

                e.Handled = true;
            }
        }

        private void ToggleDetails()
        {
            _isDetailsOpen = !_isDetailsOpen;
            DetailsPopup.Visibility = _isDetailsOpen ? Visibility.Visible : Visibility.Collapsed;
            ClickAwayOverlay.Visibility = _isDetailsOpen ? Visibility.Visible : Visibility.Collapsed;

            if (_isDetailsOpen)
            {
                UpdateAllMetrics();
            }
        }

        private void ClickAwayOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDetailsOpen)
            {
                _isDetailsOpen = false;
                DetailsPopup.Visibility = Visibility.Collapsed;
                ClickAwayOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ConstrainButtonPosition()
        {
            if (FloatingButton.ActualWidth == 0 || _parentWindow == null) return;

            double maxX = _parentWindow.ActualWidth - FloatingButton.ActualWidth - FloatingButton.Margin.Left;
            double maxY = _parentWindow.ActualHeight - FloatingButton.ActualHeight - FloatingButton.Margin.Top;
            double minX = -FloatingButton.Margin.Left;
            double minY = -FloatingButton.Margin.Top;

            FloatingButtonTransform.X = Math.Max(minX, Math.Min(maxX, FloatingButtonTransform.X));
            FloatingButtonTransform.Y = Math.Max(minY, Math.Min(maxY, FloatingButtonTransform.Y));
            DetailsPopupTransform.X = FloatingButtonTransform.X;
            DetailsPopupTransform.Y = FloatingButtonTransform.Y;
        }
    }
}
