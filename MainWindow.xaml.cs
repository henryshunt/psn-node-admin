using Newtonsoft.Json;
using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;

namespace psn_node_admin
{
    public partial class MainWindow : Window
    {
        private SerialPort Port = null;
        private NodeConfig LoadedConfig = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SerialDeviceConnected();
        }


        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            NodeConfig newConfig = new NodeConfig();
            newConfig.NetworkName = TextBoxNetworkName.Text;
            newConfig.IsEnterpriseNetwork = (bool)CheckBoxIsEnterprise.IsChecked;
            newConfig.NetworkUsername = TextBoxNetworkUsername.Text;
            newConfig.NetworkPassword = TextBoxNetworkPassword.Text;
            newConfig.LoggerAddress = TextBoxLoggerAddress.Text;
            newConfig.LoggerPort = Convert.ToUInt16(TextBoxLoggerPort.Text);
            newConfig.NetworkConnectTimeout = (byte)SliderNetworkConnectTimeout.Value;
            newConfig.LoggerConnectTimeout = (byte)SliderLoggerConnectTimeout.Value;
            newConfig.LoggerSubscribeTimeout = (byte)SliderLoggerSubscribeTimeout.Value;
            newConfig.LoggerSessionTimeout = (byte)SliderLoggerSessionTimeout.Value;
            newConfig.LoggerReportTimeout = (byte)SliderLoggerReportTimeout.Value;

            Console.WriteLine(newConfig.ToString());
            Port.Write("psna_wc " + newConfig.ToString() + '\n');
            WaitForSerialMessage();

            ButtonCancel_Click(this, new RoutedEventArgs());
        }
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (Port != null)
            {
                Port.Close();
                Port = null;
            }

            LoadedConfig = null;
            GridConfigOverlay.Visibility = Visibility.Hidden;
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
        private void TextBoxNetworkPassword_TextChanged(object sender, TextChangedEventArgs e)
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
                    if (TextBoxNetworkPassword.Text.Length > 0)
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

        private void SerialDeviceConnected()
        {
            try
            {
                Port = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);
                Port.Open();

                // Send start communication (SC) command to check if device is a PSN node
                Port.Write("psna_sc\n");

                // Received SC command response so send read config (RC) command
                if (WaitForSerialMessage() == "psna_scr")
                {
                    Port.Write("psna_rc\n");
                    string rcResponse = WaitForSerialMessage();

                    // Received RC command response with JSON data. Display to user
                    if (rcResponse.StartsWith("psna_rcr {"))
                    {
                        string json = rcResponse.Replace("psna_rcr ", "");
                        LoadedConfig = JsonConvert.DeserializeObject<NodeConfig>(json);

                        LabelID.Content = "Node ID/MAC Address: " + LoadedConfig.Identifier;
                        TextBoxNetworkName.Text = LoadedConfig.NetworkName;
                        CheckBoxIsEnterprise.IsChecked = LoadedConfig.IsEnterpriseNetwork;
                        TextBoxNetworkUsername.Text = LoadedConfig.NetworkUsername;
                        TextBoxNetworkPassword.Text = LoadedConfig.NetworkPassword;
                        TextBoxLoggerAddress.Text = LoadedConfig.LoggerAddress;
                        TextBoxLoggerPort.Text = LoadedConfig.LoggerPort.ToString();
                        SliderNetworkConnectTimeout.Value = LoadedConfig.NetworkConnectTimeout;
                        SliderLoggerConnectTimeout.Value = LoadedConfig.LoggerConnectTimeout;
                        SliderLoggerSubscribeTimeout.Value = LoadedConfig.LoggerSubscribeTimeout;
                        SliderLoggerSessionTimeout.Value = LoadedConfig.LoggerSessionTimeout;
                        SliderLoggerReportTimeout.Value = LoadedConfig.LoggerReportTimeout;

                        GridConfigOverlay.Visibility = Visibility.Visible;
                    }
                }
            }
            catch
            {
                if (Port != null)
                {
                    Port.Close();
                    Port = null;
                    LoadedConfig = null;
                }
            }
        }
        private string WaitForSerialMessage()
        {
            string message = "";
            bool message_ended = false;

            while (!message_ended)
            {
                while (Port.BytesToRead > 0)
                {
                    char readChar = (char)Port.ReadChar();

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