using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace psn_node_admin
{
    public partial class MainWindow : Window
    {
        private SerialPort Port = null;
        private NodeInfo ConnectedNode { get; set; } = null;

        private bool IsAwaitingPing = false;
        private bool IsAwaitingConfig = false;
        private bool IsAwaitingWrite = false;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        // https://forum.arduino.cc/index.php?topic=396450

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Port = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);
            Port.Open();

            // Send ping to check if device is a PSN node
            IsAwaitingPing = true;
            Port.Write("psna_sc\n");

            if (WaitForResponse() == "psna_scr")
            {
                Port.Write("psna_rc\n");
                string x = WaitForResponse();
                if (x.StartsWith("psna_rcr {"))
                {
                    string json = x.Replace("psna_rcr ", "");
                    NodeInfo reportJson = JsonConvert.DeserializeObject<NodeInfo>(json);

                    LabelID.Content = "Node ID/MAC Address: " + reportJson.Identifier;

                    TextBoxNetworkName.Text = reportJson.NetworkName;
                    CheckBoxIsEnterprise.IsChecked = reportJson.IsEnterpriseNetwork;
                    TextBoxNetworkUsername.Text = reportJson.NetworkUsername;
                    TextBoxNetworkPassword.Text = reportJson.NetworkPassword;

                    TextBoxLoggerAddress.Text = reportJson.LoggerAddress;
                    TextBoxLoggerPort.Text = reportJson.LoggerPort.ToString();

                    SliderNetworkConnectTimeout.Value = reportJson.NetworkConnectTimeout;
                    SliderLoggerConnectTimeout.Value = reportJson.LoggerConnectTimeout;
                    SliderLoggerSubscribeTimeout.Value = reportJson.LoggerSubscribeTimeout;
                    SliderLoggerSessionTimeout.Value = reportJson.LoggerSessionTimeout;
                    SliderLoggerReportTimeout.Value = reportJson.LoggerReportTimeout;


                    GridConfigOverlay.Visibility = Visibility.Visible;
                }
            }
        }

        private string WaitForResponse()
        {
            string response = "";
            bool should_end = false;

            while (!should_end)
            {
                while (Port.BytesToRead > 0)
                {
                    char c = (char)Port.ReadChar();

                    if (c != '\n')
                        response += c;
                    else should_end = true;
                }
            }

            Console.WriteLine(response);
            return response;
        }


        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            GridConfigOverlay.Visibility = Visibility.Collapsed;
        }
    }
}