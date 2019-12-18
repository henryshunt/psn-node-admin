using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace PSNNodeAdmin.Routines
{
    // Developed in tandem with a very similar class in https://github.com/henryshunt/dcim-ingester
    // Original code from https://stackoverflow.com/questions/1976573/using-registerdevicenotification-in-a-net-app
    public class SerialPortWatcher
    {
        private DeviceChangeWatcher DeviceWatcher = new DeviceChangeWatcher();

        private List<string> serialPorts = new List<string>();
        public IReadOnlyCollection<string> SerialPorts
        {
            get { return serialPorts.AsReadOnly(); }
        }

        private int MessagesToProcess = 0;
        bool isHandlingMessage = false;

        public event EventHandler<SerialPortChangedEventArgs> SerialPortAdded;
        public event EventHandler<SerialPortChangedEventArgs> SerialPortRemoved;

        private object MessageHandleLock = new object();

        public SerialPortWatcher()
        {

        }


        public void StartWatching(HwndSource windowHandle)
        {
            windowHandle.AddHook(WindowMessageHandler);
            serialPorts = GetSerialPorts();

            DeviceWatcher.RegisterDeviceNotification(windowHandle.Handle,
                new Guid("86E0D1E0-8089-11D0-9CE4-08003E301F73")); // COM ports
        }
        public void StopWatching()
        {
            DeviceWatcher.UnregisterDeviceNotification();
            serialPorts = null;
        }

        private IntPtr WindowMessageHandler(
            IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            // Only process device addition and removal messages
            if (msg == DeviceChangeWatcher.WM_DEVICE_CHANGE
                && (int)wparam == DeviceChangeWatcher.DBT_DEVICE_ARRIVAL
                || (int)wparam == DeviceChangeWatcher.DBT_DEVICE_REMOVE_COMPLETE)
            {
                DeviceChangeWatcher.DevBroadcastDeviceInterface dbdi
                    = (DeviceChangeWatcher.DevBroadcastDeviceInterface)
                    Marshal.PtrToStructure(lparam, typeof(DeviceChangeWatcher
                    .DevBroadcastDeviceInterface));

                if (dbdi.Name.StartsWith("\\\\?\\")) // Ignore invalid false positives
                {
                    lock (MessageHandleLock)
                    {
                        MessagesToProcess++;
                        if (!isHandlingMessage)
                        {
                            isHandlingMessage = true;

                            // Handle message in new thread to allow this method to return
                            new Thread(delegate () { SerialPortsChanged(); }).Start();
                        }
                    }
                }
            }

            handled = false;
            return IntPtr.Zero;
        }
        private void SerialPortsChanged()
        {
            List<string> newSerialPorts = GetSerialPorts();

            // Check for any added serial ports
            foreach (string serialPort in newSerialPorts)
            {
                if (!SerialPorts.Contains(serialPort))
                {
                    Application.Current.Dispatcher.Invoke(delegate ()
                    {
                        SerialPortAdded?.Invoke(
                            this, new SerialPortChangedEventArgs(serialPort));
                    });
                }
            }

            // Check for any removed serial ports
            foreach (string serialPort in SerialPorts)
            {
                if (!newSerialPorts.Contains(serialPort))
                {
                    Application.Current.Dispatcher.Invoke(delegate ()
                    {
                        SerialPortRemoved?.Invoke(
                            this, new SerialPortChangedEventArgs(serialPort));
                    });
                }
            }

            serialPorts = newSerialPorts;
            lock (MessageHandleLock)
            {
                MessagesToProcess--;

                // Could have received another message while processing this message
                if (MessagesToProcess == 0)
                {
                    isHandlingMessage = false;
                    return;
                }
            }

            SerialPortsChanged();
        }

        private List<string> GetSerialPorts()
        {
            List<string> newSerialPorts = new List<string>();
            ManagementObjectSearcher query = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_SerialPort");

            foreach (ManagementObject serialPort in query.Get())
                newSerialPorts.Add(serialPort["DeviceID"].ToString());
            return newSerialPorts;
        }


        public class SerialPortChangedEventArgs : EventArgs
        {
            public string SerialPortID { get; private set; }

            public SerialPortChangedEventArgs(string serialPortID)
            {
                SerialPortID = serialPortID;
            }
        }
    }
}
