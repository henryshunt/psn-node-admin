using Newtonsoft.Json;
using System;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using static psn_node_admin.DeviceWatcher;

namespace psn_node_admin
{
    public partial class MainWindow : Window
    {
        private DeviceWatcher Devices = new DeviceWatcher();

        private bool isSessionOpen = false;
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
        private void Devices_DeviceAdded(object sender, DeviceChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (isSessionOpen) return;
            isSessionOpen = true;

            Thread.Sleep(100);
            if (ConnectToDevice(e.DeviceID))
            {
                if (GetDeviceConfig())
                {
                    LabelID.Content = "Node ID/MAC Address: " + DeviceConfig.Identifier;
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
                    GridConfigOverlay.Visibility = Visibility.Visible;
                }
                else
                {
                    if (DevicePort != null)
                    {
                        DevicePort.Close();
                        DevicePort = null;
                        isSessionOpen = false;
                    }
                }
            }
            else
            {
                if (DevicePort != null)
                {
                    DevicePort.Close();
                    DevicePort = null;
                    isSessionOpen = false;
                }
            }
        }
        private void Devices_DeviceRemoved(object sender, DeviceChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (isSessionOpen &&
                DevicePort != null && DevicePort.PortName == e.DeviceID)
            {
                DevicePort.Close();
                DevicePort = null;
                DeviceConfig = null;
                GridConfigOverlay.Visibility = Visibility.Hidden;
                isSessionOpen = false;
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Devices.StopWatching();
        }

        private bool ConnectToDevice(string comPort)
        {
            try
            {
                DevicePort = new SerialPort(
                    comPort, 9600, Parity.None, 8, StopBits.One);
                DevicePort.Open();

                // Send command to check if the device is a PSN node
                DevicePort.Write("psna_sc\n");

                if (WaitForSerialMessage() == "psna_scr")
                    return true;
            }
            catch { }

            return false;
        }
        private bool GetDeviceConfig()
        {
            DevicePort.Write("psna_rc\n");
            string response = WaitForSerialMessage();

            // Deserialise returned JSON message
            if (response.StartsWith("psna_rcr {"))
            {
                string json = response.Replace("psna_rcr ", "");
                DeviceConfig = JsonConvert.DeserializeObject<NodeConfig>(json);
                return true;
            }

            return false;
        }

        private void TextBoxNetworkName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }
        private void CheckBoxIsEnterprise_Checked(object sender, RoutedEventArgs e)
        {
            if (!(bool)CheckBoxIsEnterprise.IsChecked)
                TextBoxNetworkUsername.Text = "";
            ValidateFields();
        }
        private void TextBoxNetworkUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }
        private void PasswordBoxNetworkPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateFields();
        }
        private void TextBoxLoggerAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }
        private void TextBoxLoggerPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
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
                            if (TextBoxLoggerPort.Text.Length > 0)
                            {
                                ButtonSave.IsEnabled = true;
                                return;
                            }
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

            Console.WriteLine(newConfig.ToString());
            DevicePort.Write("psna_wc " + newConfig.ToString() + '\n');
            WaitForSerialMessage();

            ButtonCancel_Click(this, new RoutedEventArgs());
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

        private string WaitForSerialMessage()
        {
            string message = "";
            bool message_ended = false;

            while (!message_ended)
            {
                while (DevicePort.BytesToRead > 0)
                {
                    char readChar = (char)DevicePort.ReadChar();

                    if (readChar != '\n')
                        message += readChar;
                    else message_ended = true;
                }
            }

            Console.WriteLine(message);
            return message;
        }
    }
}