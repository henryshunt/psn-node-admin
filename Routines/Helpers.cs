using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO.Ports;

namespace PSNNodeAdmin.Routines
{
    internal static class Helpers
    {
        internal static SerialPort ConnectToDevice(string portName)
        {
            SerialPort port = null;
            try
            {
                port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                port.Open();

                // Send command to check if the device is a PSN node
                port.Write("psn_pn\n");

                if (WaitForSerialMessage(port) != "psn_pn")
                {
                    port.Close();
                    return null;
                }
                else return port;
            }
            catch
            {
                if (port != null)
                    port.Close();
                return null;
            }
        }

        internal static DeviceConfig ReadDeviceConfig(SerialPort port)
        {
            try
            {
                port.Write("psn_rc\n");

                string response = WaitForSerialMessage(port);
                if (response == null) return null;

                // Deserialise returned JSON message
                if (response.StartsWith("psn_rc {"))
                {
                    response = response.Replace("psn_rc ", "");
                    DeviceConfig config = JsonConvert.DeserializeObject<DeviceConfig>(response);
                    return config;
                }
                else return null;
            }
            catch { return null; }
        }

        internal static bool WriteDeviceConfig(SerialPort port, DeviceConfig config)
        {
            try
            {
                port.Write("psn_wc " + config.ToString() + "\n");

                string response = WaitForSerialMessage(port);
                return response == "psn_wcs" ? true : false;
            }
            catch { return false; }
        }

        internal static DeviceTime ReadDeviceTime(SerialPort port)
        {
            try
            {
                port.Write("psn_rt\n");

                string response = WaitForSerialMessage(port);
                if (response == null) return null;

                // Deserialise returned JSON message
                if (response.StartsWith("psn_rt {"))
                {
                    response = response.Replace("psn_rt ", "");
                    DeviceTime time = JsonConvert.DeserializeObject<DeviceTime>(response);
                    return time;
                }
                else return null;
            }
            catch { return null; }
        }

        internal static bool WriteDeviceTime(SerialPort port)
        {
            try
            {
                // Send command at start of next second as we can't send a value with more
                // precision than seconds
                while (DateTime.Now.Millisecond > 0) ;

                double secondsSince2000 = Math.Floor(
                    (DateTime.UtcNow - new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);

                port.Write("psn_wt {\"time\":" + secondsSince2000 + "}\n");

                string response = WaitForSerialMessage(port);
                return response == "psn_wts" ? true : false;
            }
            catch { return false; }
        }

        internal static string WaitForSerialMessage(SerialPort port)
        {
            string message = "";
            bool messageEnded = false;

            Stopwatch timeout = new Stopwatch();
            timeout.Start();

            while (!messageEnded)
            {
                while (port.BytesToRead > 0)
                {
                    char readChar = (char)port.ReadChar();

                    if (readChar != '\n')
                        message += readChar;
                    else messageEnded = true;
                }

                if (timeout.ElapsedMilliseconds >= 1000)
                    return null;
            }

            Console.WriteLine(message);
            return message;
        }
    }
}
