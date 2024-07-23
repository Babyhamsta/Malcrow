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
            UpdateCPUUsage(90);
            UpdateRAMUsage(5);
        }

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
            FirstFlyout.IsOpen = !FirstFlyout.IsOpen;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            FirstFlyout.IsOpen = false;
        }

        private void UpdateCPUUsage(double usage)
        {
            double percentage = usage / 100.0;
            double fullCircle = 28.3;
            CPUPath.StrokeDashArray = new DoubleCollection { fullCircle * percentage, fullCircle * (1 - percentage) };
            CPUPercent.Content = $"{usage.ToString()}%";
        }

        private void UpdateRAMUsage(double usage)
        {
            double percentage = usage / 100.0;
            double fullCircle = 28.3;
            RAMPath.StrokeDashArray = new DoubleCollection { fullCircle * percentage, fullCircle * (1 - percentage) };
            RAMPercent.Content = $"{usage.ToString()}%";
        }
    }
}
