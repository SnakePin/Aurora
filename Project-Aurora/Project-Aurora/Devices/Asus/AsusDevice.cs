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

        private int ledCount = 0;
        byte[] colors;


        ClaymoreSdk keyboard;

        public bool Initialize()
        {

            keyboard = new ClaymoreSdk();
            if (!keyboard.Start())
            {
                Global.logger.Error("Asus: Failed to load DLL");
                isConnected = false;
                return false;
            }
            ledCount = keyboard.GetLedCount() * 3;
            colors = new byte[ledCount];

            Global.logger.Info("Got Claymore Keyboard: " + ledCount + " leds");
            keyboard.SetToSWMode();     // Take control of the keyboard

            isConnected = true;
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
            return isConnected;
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
            return true;
        }

        public void Reset()
        {
            keyboard.Stop();
            keyboard.Start();
        }

        public void Shutdown()
        {
            isInitialized = false;
            keyboard.Stop();
        }

        private void setColorMatrix()
        {
            keyboard.setKeyboardColor(colors);
        }

        private void updateClaymoreKeyColor(DeviceKeys key, Color color)
        {

                colors[KeyToClaymoreLedID(key)] = color.G;  // GREEN
                colors[KeyToClaymoreLedID(key) + 1] = color.B; // BLUE
                colors[KeyToClaymoreLedID(key) + 2] = color.R;  // RED

            //Apply and strip Alpha
            //color = Color.FromArgb(255, Utils.ColorUtils.MultiplyColorByScalar(color, color.A / 255.0D));
            //if (keyboard != null && keyboard[localKey] != null)
            //    keyboard[localKey].Color = color;
        }

        public bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, CancellationToken token, bool forced = false)
        {
            if (token.IsCancellationRequested) return false;
            foreach (KeyValuePair<DeviceKeys, Color> key in keyColors)
            {
                if (token.IsCancellationRequested) return false;
                updateClaymoreKeyColor(key.Key, key.Value);

            }

            if (token.IsCancellationRequested) return false;
            setColorMatrix();
            return true;
        }

        public bool UpdateDevice(DeviceColorComposition colorComposition, CancellationToken token, bool forced = false)
        {
            if (isInitialized)
            {
                watch.Restart();

                bool update_result = UpdateDevice(colorComposition.keyColors, token, forced);

                watch.Stop();
                lastUpdateTime = watch.ElapsedMilliseconds;

                return update_result;
            }
            return false;

        }

        private int KeyToClaymoreLedID(DeviceKeys key)
        {
            switch (key)
            {
                case DeviceKeys.BACKSLASH:
                    return 0;
                case DeviceKeys.TAB:
                    return 3;
                case DeviceKeys.CAPS_LOCK:
                    return 6;
                case DeviceKeys.LEFT_SHIFT:
                    return 9;
                case DeviceKeys.LEFT_CONTROL:
                    return 12;
                case DeviceKeys.F1:
                    return 15;
                case DeviceKeys.NUM_ONE:
                    return 18;
                case DeviceKeys.Q:
                    return 21;
                case DeviceKeys.A:
                    return 24;
                default:
                    return 0;
            }
        }
    }
}
