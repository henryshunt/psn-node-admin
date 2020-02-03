using PSNNodeAdmin.Routines;
using System;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using static PSNNodeAdmin.Routines.SerialPortWatcher;

namespace PSNNodeAdmin.Windows
{
    public partial class MainWindow : Window
    {
        private SerialPortWatcher SerialPorts = new SerialPortWatcher();

        private bool isConnecting = false;
        private SerialPort DevicePort = null;

        public MainWindow()
        {
            InitializeComponent();
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource windowHandle = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);

            // Register for device change messages
            if (windowHandle != null)
            {
                SerialPorts.SerialPortAdded += SerialPorts_SerialPortAdded;
                SerialPorts.SerialPortRemoved += SerialPorts_SerialPortRemoved;
                SerialPorts.StartWatching(windowHandle);
            }
        }

        private void SerialPorts_SerialPortAdded(object sender, SerialPortChangedEventArgs e)
        {
            if (!IsLoaded || isConnecting || DevicePort != null) return;
            isConnecting = true;

            Thread.Sleep(100); // Allow the device to finish starting up
            DevicePort = Helpers.ConnectToDevice(e.SerialPortID);
            Thread.Sleep(100);

            if (DevicePort != null)
            {
                DeviceConfig config = Helpers.ReadDeviceConfig(DevicePort);
                if (config != null)
                {
                    LabelID.Content = "Device MAC Address: " + config.MACAddress;
                    TextBoxNetworkName.Text = config.NetworkName;
                    CheckBoxIsEnterprise.IsChecked = config.IsEnterpriseNetwork;
                    TextBoxNetworkUsername.Text = config.NetworkUsername;
                    PasswordBoxNetworkPassword.Password = config.NetworkPassword;
                    TextBoxLoggerAddress.Text = config.LoggerAddress;
                    TextBoxLoggerPort.Text = config.LoggerPort.ToString();
                    SliderNetworkTimeout.Value = config.NetworkTimeout;
                    SliderLoggerTimeout.Value = config.LoggerTimeout;

                    LoadDeviceTime(false);

                    ButtonSave.IsEnabled = false;
                    GridConfigOverlay.Visibility = Visibility.Visible;
                    TextBoxNetworkName.Focus();
                }
                else
                {
                    if (DevicePort != null)
                    {
                        DevicePort.Close();
                        DevicePort = null;
                        isConnecting = false;
                    }

                    MessageBox.Show("Unable to read device configuration.");
                }
            }

            isConnecting = false;
        }
        private void SerialPorts_SerialPortRemoved(object sender, SerialPortChangedEventArgs e)
        {
            if (!IsLoaded || isConnecting) return;
            if (DevicePort != null && DevicePort.PortName == e.SerialPortID)
            {
                DevicePort.Close();
                DevicePort = null;

                GridConfigOverlay.Visibility = Visibility.Hidden;
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            SerialPorts.StopWatching();

            if (DevicePort != null)
                DevicePort.Close();
        }

        private void ButtonRefreshTime_Click(object sender, RoutedEventArgs e)
        {
            LoadDeviceTime(true);
        }
        private void LoadDeviceTime(bool failureMessage)
        {
            DeviceTime time = Helpers.ReadDeviceTime(DevicePort);
            if (time != null)
            {
                TextBoxDeviceTime.Text = DateTime.Parse(time.Time).ToString(
                    "dd/MM/yyyy HH:mm:ss") + " UTC (" + (time.IsTimeValid ? "VALID" : "INVALID") + ")";
            }
            else
            {
                TextBoxDeviceTime.Text = "Unable to read device time";
                if (failureMessage)
                    MessageBox.Show("Unable to read device time.");
            }
        }
        private void ButtonResyncTime_Click(object sender, RoutedEventArgs e)
        {
            if (Helpers.WriteDeviceTime(DevicePort))
                LoadDeviceTime(true);
            else MessageBox.Show("Unable to write time to device.");
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            DeviceConfig newConfig = new DeviceConfig();
            newConfig.NetworkName = TextBoxNetworkName.Text;
            newConfig.IsEnterpriseNetwork = (bool)CheckBoxIsEnterprise.IsChecked;
            newConfig.NetworkUsername = TextBoxNetworkUsername.Text;
            newConfig.NetworkPassword = PasswordBoxNetworkPassword.Password;
            newConfig.LoggerAddress = TextBoxLoggerAddress.Text;
            newConfig.LoggerPort = Convert.ToInt32(TextBoxLoggerPort.Text);
            newConfig.NetworkTimeout = (int)SliderNetworkTimeout.Value;
            newConfig.LoggerTimeout = (int)SliderLoggerTimeout.Value;

            if (Helpers.WriteDeviceConfig(DevicePort, newConfig))
                ButtonFinish_Click(this, new RoutedEventArgs());
            else MessageBox.Show("Unable to write configuration to device.");
        }
        private void ButtonFinish_Click(object sender, RoutedEventArgs e)
        {
            if (DevicePort != null)
            {
                DevicePort.Close();
                DevicePort = null;
            }

            GridConfigOverlay.Visibility = Visibility.Hidden;
        }


        private void TextBoxField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (GridConfigOverlay.Visibility != Visibility.Visible) return;
            ValidateFields();
        }
        private void CheckBoxField_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (GridConfigOverlay.Visibility != Visibility.Visible) return;

            if (!(bool)CheckBoxIsEnterprise.IsChecked)
                TextBoxNetworkUsername.Text = "";

            ValidateFields();
        }
        private void PasswordBoxField_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (GridConfigOverlay.Visibility != Visibility.Visible) return;
            ValidateFields();
        }
        private void SliderField_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GridConfigOverlay.Visibility != Visibility.Visible) return;
            ValidateFields();
        }
        private void ValidateFields()
        {
            if (TextBoxNetworkName.Text.Length > 0)
            {
                if (!(bool)CheckBoxIsEnterprise.IsChecked || ((bool)CheckBoxIsEnterprise.IsChecked &&
                    TextBoxNetworkUsername.Text.Length > 0 && PasswordBoxNetworkPassword.Password.Length > 0))
                {
                    if (TextBoxLoggerAddress.Text.Length > 0)
                    {
                        try
                        {
                            if (Convert.ToUInt16(TextBoxLoggerPort.Text) >= 1024)
                            {
                                ButtonSave.IsEnabled = true;
                                return;
                            }
                        }
                        catch { }
                    }
                }
            }

            ButtonSave.IsEnabled = false;
        }
    }
}