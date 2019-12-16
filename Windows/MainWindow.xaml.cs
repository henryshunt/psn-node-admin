using Newtonsoft.Json;
using PSNNodeAdmin.Routines;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace PSNNodeAdmin.Windows
{
    public partial class MainWindow : Window
    {
        private DeviceWatcher Devices = new DeviceWatcher();

        private bool isConnecting = false;
        private SerialPort DevicePort = null;
        private NodeConfig DeviceConfig;

        public MainWindow()
        {
            InitializeComponent();
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);

            // Register for device change messages
            if (source != null)
            {
                Devices.DeviceAdded += Devices_DeviceAdded;
                Devices.DeviceRemoved += Devices_DeviceRemoved;
                Devices.StartWatching(source);
            }
        }
        private void Devices_DeviceAdded(
            object sender, DeviceWatcher.DeviceChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (isConnecting) return;
            isConnecting = true;

            Thread.Sleep(100);
            if (ConnectToDevice(e.DeviceID))
            {
                if (GetDeviceConfig())
                {
                    LabelID.Content = "Node MAC Address: " + DeviceConfig.MACAddress;
                    TextBoxNetworkName.Text = DeviceConfig.NetworkName;
                    CheckBoxIsEnterprise.IsChecked = DeviceConfig.IsEnterpriseNetwork;
                    TextBoxNetworkUsername.Text = DeviceConfig.NetworkUsername;
                    PasswordBoxNetworkPassword.Password = DeviceConfig.NetworkPassword;
                    TextBoxLoggerAddress.Text = DeviceConfig.LoggerAddress;
                    TextBoxLoggerPort.Text = DeviceConfig.LoggerPort.ToString();
                    SliderNetworkConnectTimeout.Value = DeviceConfig.NetworkConnectTimeout;
                    SliderLoggerConnectTimeout.Value = DeviceConfig.LoggerConnectTimeout;
                    SliderLoggerSubscribeTimeout.Value = DeviceConfig.LoggerSubscribeTimeout;
                    SliderLoggerSessionTimeout.Value = DeviceConfig.LoggerSessionTimeout;
                    SliderLoggerReportTimeout.Value = DeviceConfig.LoggerReportTimeout;

                    ButtonSave.IsEnabled = false;
                    GridConfigOverlay.Visibility = Visibility.Visible;
                    isConnecting = false;
                }
                else
                {
                    if (DevicePort != null)
                    {
                        DevicePort.Close();
                        DevicePort = null;
                        isConnecting = false;
                    }
                }
            }
            else
            {
                if (DevicePort != null)
                {
                    DevicePort.Close();
                    DevicePort = null;
                    isConnecting = false;
                }
            }
        }
        private void Devices_DeviceRemoved(
            object sender, DeviceWatcher.DeviceChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (isConnecting) return;

            if (DevicePort != null && DevicePort.PortName == e.DeviceID)
            {
                DevicePort.Close();
                DevicePort = null;
                DeviceConfig = null;

                GridConfigOverlay.Visibility = Visibility.Hidden;
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Devices.StopWatching();

            if (DevicePort != null)
                DevicePort.Close();
        }

        private void TextBoxField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (GridConfigOverlay.Visibility != Visibility.Visible) return;

            ValidateFields();
        }
        private void CheckBoxField_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (GridConfigOverlay.Visibility != Visibility.Visible) return;

            if (!(bool)CheckBoxIsEnterprise.IsChecked)
                TextBoxNetworkUsername.Text = "";
            ValidateFields();
        }
        private void PasswordBoxField_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (GridConfigOverlay.Visibility != Visibility.Visible) return;

            ValidateFields();
        }
        private void SliderField_ValueChanged(
            object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            if (GridConfigOverlay.Visibility != Visibility.Visible) return;

            ButtonSave.IsEnabled = true;
        }

        private void ValidateFields()
        {
            if (TextBoxNetworkName.Text.Length > 0)
            {
                if (!(bool)CheckBoxIsEnterprise.IsChecked ||
                    ((bool)CheckBoxIsEnterprise.IsChecked && TextBoxNetworkUsername.Text.Length > 0))
                {
                    if (PasswordBoxNetworkPassword.Password.Length > 0)
                    {
                        if (TextBoxLoggerAddress.Text.Length > 0)
                        {
                            try
                            {
                                Convert.ToUInt16(TextBoxLoggerPort.Text);

                                ButtonSave.IsEnabled = true;
                                return;
                            }
                            catch { }
                        }
                    }
                }
            }

            ButtonSave.IsEnabled = false;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            NodeConfig newConfig = new NodeConfig();
            newConfig.NetworkName = TextBoxNetworkName.Text;
            newConfig.IsEnterpriseNetwork = (bool)CheckBoxIsEnterprise.IsChecked;
            newConfig.NetworkUsername = TextBoxNetworkUsername.Text;
            newConfig.NetworkPassword = PasswordBoxNetworkPassword.Password;
            newConfig.LoggerAddress = TextBoxLoggerAddress.Text;
            newConfig.LoggerPort = Convert.ToUInt16(TextBoxLoggerPort.Text);
            newConfig.NetworkConnectTimeout = (byte)SliderNetworkConnectTimeout.Value;
            newConfig.LoggerConnectTimeout = (byte)SliderLoggerConnectTimeout.Value;
            newConfig.LoggerSubscribeTimeout = (byte)SliderLoggerSubscribeTimeout.Value;
            newConfig.LoggerSessionTimeout = (byte)SliderLoggerSessionTimeout.Value;
            newConfig.LoggerReportTimeout = (byte)SliderLoggerReportTimeout.Value;

            try
            {
                DevicePort.Write("psna_wc " + newConfig.ToString() + '\n');

                string response = WaitForSerialMessage();
                if (response == "psna_wcs")
                    ButtonCancel_Click(this, new RoutedEventArgs());
                else throw new Exception("Write config command did not succeed");
            }
            catch { MessageBox.Show("Error while writing configuration to device"); }
        }
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (DevicePort != null)
            {
                DevicePort.Close();
                DevicePort = null;
            }

            DeviceConfig = null;
            GridConfigOverlay.Visibility = Visibility.Hidden;
        }

        private bool ConnectToDevice(string port)
        {
            try
            {
                DevicePort = new SerialPort(port, 9600, Parity.None, 8, StopBits.One);
                DevicePort.Open();

                // Send command to check if the device is a PSN node
                DevicePort.Write("psna_pn\n");

                return WaitForSerialMessage() == "psna_pnr" ? true : false;
            }
            catch { }

            return false;
        }
        private bool GetDeviceConfig()
        {
            try
            {
                DevicePort.Write("psna_rc\n");

                string response = WaitForSerialMessage();
                if (response == null) return false;

                // Deserialise returned JSON message
                if (response.StartsWith("psna_rcr {"))
                {
                    response = response.Replace("psna_rcr ", "");
                    DeviceConfig = JsonConvert.DeserializeObject<NodeConfig>(response);
                    return true;
                }
            }
            catch { }

            return false;
        }
        private string WaitForSerialMessage()
        {
            string message = "";
            bool messageEnded = false;

            Stopwatch timeout = new Stopwatch();
            timeout.Start();

            while (!messageEnded)
            {
                while (DevicePort.BytesToRead > 0)
                {
                    char readChar = (char)DevicePort.ReadChar();

                    if (readChar != '\n')
                        message += readChar;
                    else messageEnded = true;
                }

                if (timeout.ElapsedMilliseconds >= 1000)
                    return null;
            }

            return message;
        }
    }
}