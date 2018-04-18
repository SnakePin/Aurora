using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Settings;
using System.Runtime.InteropServices;
using ClaymoreWrapper;


namespace Aurora.Devices.Asus
{
    class AsusDevice : Device
    {

        private String devicename = "Asus";
        private bool isInitialized = false;
        private bool wasInitializedOnce = false;
        private bool isConnected = false;

        private bool keyboard_updated = false;
        private bool peripheral_updated = false;

        private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private Color previous_peripheral_Color = Color.Black;
        private long lastUpdateTime = 0;

        ClaymoreSdk keyboard;

        public bool Initialize()
        {

            keyboard = new ClaymoreSdk();
            if (!keyboard.Start())
            {
                Global.logger.Error("Asus: Failed to load DLL");
                isConnected = true;
                return false;
            }

            keyboard.SetToSWMode();     // Take control of the keyboard

            isInitialized = true;
            wasInitializedOnce = true;
            return true;
        }

        public string GetDeviceDetails()
        {
            if (isInitialized)
            {
                return devicename + ": Initialized";
            }
            else
            {
                return devicename + ": Not initialized";
            }
        }

        public string GetDeviceName()
        {
            return devicename;
        }

        public string GetDeviceUpdatePerformance()
        {
            return (isInitialized ? lastUpdateTime + " ms" : "");
        }

        public VariableRegistry GetRegisteredVariables()
        {
            return new VariableRegistry();
        }

        public bool IsConnected()
        {
            return IsConnected;
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }

        public bool IsKeyboardConnected()
        {
            return true;
        }

        public bool IsPeripheralConnected()
        {
            return true;
        }

        public bool Reconnect()
        {
            keyboard.Stop();
            keyboard.Start();
        }

        public void Reset()
        {
            keyboard.Stop();
            keyboard.Start();
        }

        public void Shutdown()
        {
            keyboard.SetToHWMode();
            keyboard.Stop();
        }

        public bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, CancellationToken token, bool forced = false)
        {

            return true;
        }

        public bool UpdateDevice(DeviceColorComposition colorComposition, CancellationToken token, bool forced = false)
        {
            watch.Restart();

            bool update_result = UpdateDevice(colorComposition.keyColors, token, forced);

            watch.Stop();
            lastUpdateTime = watch.ElapsedMilliseconds;

            return update_result;
        }
    }
}
