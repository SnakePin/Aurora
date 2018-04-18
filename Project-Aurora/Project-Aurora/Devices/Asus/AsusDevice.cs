using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Settings;
using System.Runtime.InteropServices;
using AuraSDKWrapper;

namespace Aurora.Devices.Asus
{
    class AsusDevice : Device
    {

        private String devicename = "Asus";
        private bool isInitialized = false;
        private bool wasInitializedOnce = false;

        private bool keyboard_updated = false;
        private bool peripheral_updated = false;

        private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private Color previous_peripheral_Color = Color.Black;
        private long lastUpdateTime = 0;

        public bool Initialize()
        {

            AuraSDK auraSDK = new AuraSDK();
            int result = auraSDK.DetectAuraDevices();
            if (result != 0)
            {
                Console.WriteLine("Error during initialize: " + result);
                return false;
            }

            Console.WriteLine("Asus Found " + auraSDK.MBControllersCount + " motherboard controller(s)");
            Console.WriteLine("Asus Found " + auraSDK.GPUControllersCount + " gpu controller(s)");
            Console.WriteLine("Asus Found keybaord: " + auraSDK.IsKeyboardPresent);
            Console.WriteLine("Asus Found mouse: " + auraSDK.IsMousePresent);


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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
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
