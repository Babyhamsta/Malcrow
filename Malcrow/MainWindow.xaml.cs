using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;
using System;
using System.Drawing;

namespace Malcrow
{
    /// <summary>
    /// Interaction logic for BetaUI.xaml
    /// </summary>
    public partial class BetaUI : MetroWindow
    {
        private NotifyIcon _notifyIcon;
        private Tools.Registry registry = new Tools.Registry();

        public BetaUI()
        {
            InitializeComponent();
            InitializeTrayIcon();

            // Load ViewModels
            DataContext = new SettingsViewModel();

            // Set initial CPU/RAM amounts
            UpdateCPUUsage(0);
            UpdateRAMUsage(0);
        }

        #region App Tray

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = new System.Drawing.Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Malcrow-RedSquare.ico")).Stream);
            _notifyIcon.Visible = false;
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // Create context menu for tray icon
            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("Open", (s, e) => ShowMainWindow());
            contextMenu.MenuItems.Add("Exit", (s, e) => Close());

            _notifyIcon.ContextMenu = contextMenu;
        }

        private void MinimizeToTray()
        {
            _notifyIcon.Visible = true;
            Hide();
        }

        private void ShowMainWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            _notifyIcon.Visible = false;
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        #endregion

        #region Form Buttons and Functions

        private void DragForm(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeToTray();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeToTray();
        }

        private void SettingsCog_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = false;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                MinimizeToTray();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _notifyIcon.Visible = false;
        }

        #endregion

        #region Main Screen Functions

        private void SwitchToRAMPageB_Click(object sender, RoutedEventArgs e)
        {
            CPUArea.Visibility = Visibility.Collapsed;
            RAMArea.Visibility = Visibility.Visible;
        }

        private void SwitchToCPUPageB_Click(object sender, RoutedEventArgs e)
        {
            CPUArea.Visibility = Visibility.Visible;
            RAMArea.Visibility = Visibility.Collapsed;
        }

        private void UpdateCPUUsage(double usage)
        {
            double percentage = usage / 100.0;
            double fullCircle = 47.1;
            CPUPath.StrokeDashArray = new DoubleCollection { fullCircle * percentage, fullCircle * (1 - percentage) };
            CPUPercent.Content = $"{usage.ToString()}%";
        }

        private void UpdateRAMUsage(double usage)
        {
            double percentage = usage / 100.0;
            double fullCircle = 47.1;
            RAMPath.StrokeDashArray = new DoubleCollection { fullCircle * percentage, fullCircle * (1 - percentage) };
            RAMPercent.Content = $"{usage.ToString()}%";
        }

        #endregion
    }
}
