using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Aurora.Settings;
using ClaymoreWrapper;


namespace Aurora.Devices.Asus
{
    class AsusDevice : Device
    {

        private String devicename = "Claymore";
        private bool isInitialized = false;

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
                return false;
            }
            ledCount = keyboard.GetLedCount() * 3;  // R,G,B
            colors = new byte[ledCount];    // check if need to reinitialize when the numpad is disconnected

            Global.logger.Info("Got Claymore Keyboard: " + ledCount + " leds");
            keyboard.SetToSWMode();     // Take control of the keyboard

            isInitialized = true;
            return isInitialized;
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
            return isInitialized;
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }

        public bool IsKeyboardConnected()
        {
            if (!isInitialized)
            {
                keyboard = new ClaymoreSdk();
                if (!keyboard.Start())
                {
                    Global.logger.Error("Asus: Failed to load DLL");
                    return false;
                }
                keyboard.Stop();
                return true;
            }
            return false;
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

            colors[KeyToClaymoreLedID(key)] = color.R; // RED
            colors[KeyToClaymoreLedID(key) + 1] = color.G; // GREEN
            colors[KeyToClaymoreLedID(key) + 2] = color.B;  // BLU

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
                //Global.logger.Info("TASTO: " + key.Key + " val: " + key.Value);
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
                case DeviceKeys.ESC:
                    return 0;
                case DeviceKeys.TILDE:
                    return 3;
                case DeviceKeys.TAB:
                    return 6;
                case DeviceKeys.CAPS_LOCK:
                    return 9;
                case DeviceKeys.LEFT_SHIFT:
                    return 12;
                case DeviceKeys.LEFT_CONTROL:
                    return 15;
                case DeviceKeys.ONE:
                    return 27;
                case DeviceKeys.Q:
                    return 30;
                case DeviceKeys.A:
                    return 33;
                case DeviceKeys.BACKSLASH_UK:
                    return 3;
                case DeviceKeys.LEFT_WINDOWS:
                    return 39;
                case DeviceKeys.F1:
                    return 48;
                case DeviceKeys.TWO:
                    return 51;
                case DeviceKeys.W:
                    return 54;
                case DeviceKeys.S:
                    return 57;
                case DeviceKeys.Z:
                    return 60;
                case DeviceKeys.LEFT_ALT:
                    return 63;
                case DeviceKeys.F2:
                    return 72;
                case DeviceKeys.THREE:
                    return 75;
                case DeviceKeys.E:
                    return 78;
                case DeviceKeys.D:
                    return 81;
                case DeviceKeys.X:
                    return 84;
                case DeviceKeys.F3:
                    return 96;
                case DeviceKeys.FOUR:
                    return 99;
                case DeviceKeys.R:
                    return 102;
                case DeviceKeys.F:
                    return 105;
                case DeviceKeys.C:
                    return 108;
                case DeviceKeys.SPACE:
                    return 111;
                case DeviceKeys.F4:
                    return 120;
                case DeviceKeys.FIVE:
                    return 123;
                case DeviceKeys.T:
                    return 126;
                case DeviceKeys.G:
                    return 129;
                case DeviceKeys.V:
                    return 132;
                case DeviceKeys.SIX:
                    return 147;
                case DeviceKeys.Y:
                    return 150;
                case DeviceKeys.H:
                    return 153;
                case DeviceKeys.B:
                    return 156;
                case DeviceKeys.F5:
                    return 168;
                case DeviceKeys.SEVEN:
                    return 171;
                case DeviceKeys.U:
                    return 174;
                case DeviceKeys.J:
                    return 177;
                case DeviceKeys.N:
                    return 180;
                case DeviceKeys.F6:
                    return 192;
                case DeviceKeys.EIGHT:
                    return 195;
                case DeviceKeys.I:
                    return 198;
                case DeviceKeys.K:
                    return 201;
                case DeviceKeys.M:
                    return 204;
                case DeviceKeys.LOGO:
                    return 207;
                case DeviceKeys.F7:
                    return 216;
                case DeviceKeys.NINE:
                    return 219;
                case DeviceKeys.O:
                    return 222;
                case DeviceKeys.L:
                    return 225;
                case DeviceKeys.COMMA:
                    return 228;
                case DeviceKeys.RIGHT_ALT:
                    return 231;
                case DeviceKeys.F8:
                    return 240;
                case DeviceKeys.ZERO:
                    return 243;
                case DeviceKeys.P:
                    return 246;
                case DeviceKeys.SEMICOLON:
                    return 270;
                case DeviceKeys.PERIOD:
                    return 252;
                case DeviceKeys.FN_Key:
                    return 255;
                case DeviceKeys.MINUS:
                    return 276;
                case DeviceKeys.OPEN_BRACKET:
                    return 267;
                case DeviceKeys.APOSTROPHE:
                    return 249;
                case DeviceKeys.FORWARD_SLASH:
                    return 297;
                case DeviceKeys.APPLICATION_SELECT:
                    return 279;
                case DeviceKeys.F9:
                    return 288;
                case DeviceKeys.EQUALS:
                    return 294;
                case DeviceKeys.CLOSE_BRACKET:
                    return 291;
                case DeviceKeys.HASHTAG:
                    return 273;
                case DeviceKeys.F10:
                    return 312;
                case DeviceKeys.BACKSPACE:
                    return 315;
                case DeviceKeys.ENTER:
                    return 321;
                case DeviceKeys.RIGHT_SHIFT:
                    return 324;
                case DeviceKeys.RIGHT_CONTROL:
                    return 327;
                case DeviceKeys.F11:
                    return 336;
                case DeviceKeys.F12:
                    return 360;
                case DeviceKeys.PRINT_SCREEN:
                    return 384;
                case DeviceKeys.INSERT:
                    return 387;
                case DeviceKeys.DELETE:
                    return 390;
                case DeviceKeys.ARROW_LEFT:
                    return 399;
                case DeviceKeys.SCROLL_LOCK:
                    return 408;
                case DeviceKeys.HOME:
                    return 411;
                case DeviceKeys.END:
                    return 414;
                case DeviceKeys.ARROW_UP:
                    return 420;
                case DeviceKeys.ARROW_DOWN:
                    return 423;
                case DeviceKeys.PAUSE_BREAK:
                    return 432;
                case DeviceKeys.PAGE_UP:
                    return 435;
                case DeviceKeys.PAGE_DOWN:
                    return 438;
                case DeviceKeys.ARROW_RIGHT:
                    return 447;
                case DeviceKeys.NUM_LOCK:
                    return 459;
                case DeviceKeys.NUM_SEVEN:
                    return 462;
                case DeviceKeys.NUM_FOUR:
                    return 465;
                case DeviceKeys.NUM_ONE:
                    return 468;
                case DeviceKeys.NUM_ZERO:
                    return 471;
                case DeviceKeys.NUM_SLASH:
                    return 483;
                case DeviceKeys.NUM_EIGHT:
                    return 486;
                case DeviceKeys.NUM_FIVE:
                    return 489;
                case DeviceKeys.NUM_ASTERISK:
                    return 507;
                case DeviceKeys.NUM_NINE:
                    return 510;
                case DeviceKeys.NUM_SIX:
                    return 513;
                case DeviceKeys.NUM_THREE:
                    return 516;
                case DeviceKeys.NUM_MINUS:
                    return 531;
                case DeviceKeys.NUM_PLUS:
                    return 534;
                case DeviceKeys.NUM_ENTER:
                    return 540;

                default:
                    return 0;
            }
        }
    }
}
