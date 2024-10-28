using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace Malcrow
{
    public partial class MainWindow : Window
    {
        private Tools.Registry registry = new Tools.Registry();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton button = sender as ToggleButton;
            if (button != null)
            {
                switch (button.Name)
                {
                    case "VMSpoofingButton":
                        VMSpoofingOn();
                        break;
                    case "DecompileSpoofingButton":
                        DecompileSpoofingOn();
                        break;
                    case "DebugSpoofingButton":
                        DebugSpoofingOn();
                        break;
                    case "RegistrySpoofingButton":
                        RegistrySpoofingOn();
                        break;
                }
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton button = sender as ToggleButton;
            if (button != null)
            {
                switch (button.Name)
                {
                    case "VMSpoofingButton":
                        VMSpoofingOff();
                        break;
                    case "DecompileSpoofingButton":
                        DecompileSpoofingOff();
                        break;
                    case "DebugSpoofingButton":
                        DebugSpoofingOff();
                        break;
                    case "RegistrySpoofingButton":
                        RegistrySpoofingOff();
                        break;
                }
            }
        }

        private void VMSpoofingOn()
        {
            // Implement the logic for VM Spoofing On
        }

        private void VMSpoofingOff()
        {
            // Implement the logic for VM Spoofing Off
        }

        private void DecompileSpoofingOn()
        {
            // Implement the logic for Decompile Spoofing On
        }

        private void DecompileSpoofingOff()
        {
            // Implement the logic for Decompile Spoofing Off
        }

        private void DebugSpoofingOn()
        {
            // Implement the logic for Debug Spoofing On
        }

        private void DebugSpoofingOff()
        {
            // Implement the logic for Debug Spoofing Off
        }

        private void RegistrySpoofingOn()
        {
            // Implement the logic for Registry Spoofing On
        }

        private void RegistrySpoofingOff()
        {
            // Implement the logic for Registry Spoofing Off
        }
    }
}