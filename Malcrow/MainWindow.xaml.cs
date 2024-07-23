using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Media;

namespace Malcrow
{
    /// <summary>
    /// Interaction logic for BetaUI.xaml
    /// </summary>
    public partial class BetaUI : MetroWindow
    {
        private Tools.Registry registry = new Tools.Registry();

        public BetaUI()
        {
            InitializeComponent();

            // Load ViewModels
            DataContext = new SettingsViewModel();

            // Set initial CPU/RAM amounts
            UpdateCPUUsage(1);
            UpdateRAMUsage(5);
        }

        #region Form Buttons and Functions

        private void DragForm(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void SettingsCog_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = false;
        }

        #endregion

        #region Switch Statistics Pages

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

        #endregion

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
    }
}
