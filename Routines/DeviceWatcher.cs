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
    public class DeviceWatcher
    {
        private List<string> devices = new List<string>();
        public IReadOnlyCollection<string> Devices
        {
            get { return devices.AsReadOnly(); }
        }

        private int MessagesToProcess = 0;
        bool isHandlingMessage = false;

        public event EventHandler<DeviceChangedEventArgs> DeviceAdded;
        public event EventHandler<DeviceChangedEventArgs> DeviceRemoved;

        private object MessageHandleLock = new object();

        public DeviceWatcher()
        {

        }


        public void StartWatching(HwndSource windowHandle)
        {
            windowHandle.AddHook(WindowMessageHandler);
            devices = GetDevices();

            new DeviceChangeWatcher().RegisterDeviceNotification(windowHandle.Handle,
                new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED")); // USB devices
        }
        public void StopWatching()
        {
            new DeviceChangeWatcher().UnregisterDeviceNotification();
            devices = null;
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
                            new Thread(delegate () { DevicesChanged(); }).Start();
                        }
                    }
                }
            }

            handled = false;
            return IntPtr.Zero;
        }
        private void DevicesChanged()
        {
            List<string> newDevices = GetDevices();

            // Check for any added devices
            foreach (string device in newDevices)
            {
                if (!Devices.Contains(device))
                {
                    Application.Current.Dispatcher.Invoke(delegate ()
                    {
                        DeviceAdded?.Invoke(this,
                            new DeviceChangedEventArgs(device));
                    });
                }
            }

            // Check for any removed devices
            foreach (string device in Devices)
            {
                if (!newDevices.Contains(device))
                {
                    Application.Current.Dispatcher.Invoke(delegate ()
                    {
                        DeviceRemoved?.Invoke(
                            this, new DeviceChangedEventArgs(device));
                    });
                }
            }

            devices = newDevices;
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

            DevicesChanged();
        }

        private List<string> GetDevices()
        {
            List<string> newDevices = new List<string>();
            ManagementObjectSearcher query = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_SerialPort");

            foreach (ManagementObject device in query.Get())
                newDevices.Add(device["DeviceID"].ToString());
            return newDevices;
        }


        public class DeviceChangedEventArgs : EventArgs
        {
            public string DeviceID { get; private set; }

            public DeviceChangedEventArgs(string deviceId)
            {
                DeviceID = deviceId;
            }
        }
    }
}
