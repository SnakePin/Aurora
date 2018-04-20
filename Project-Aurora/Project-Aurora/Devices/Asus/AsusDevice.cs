using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Aurora.Settings;
using AsusSdkWrapper;

namespace Aurora.Devices.Asus
{
    class AsusDevice : Device
    {

        private String devicename = "Asus";
        private bool isInitialized = false;

        private readonly object action_lock = new object();
        private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private Color previous_peripheral_Color = Color.Black;
        private long lastUpdateTime = 0;

        // AURA STUFF
        AuraSdk Aura;

        bool _isKeyboardPresent = false;
        bool _isMousePresent = false;

        bool _isMbInitialized = false;
        bool _isGpuInitialized = false;
        bool _isMouseInitialized = false;
        bool _keyboardInitialized = false;

        int _MbControllers = 0;
        int _GPUSControllers = 0;

        int _keyboardLedCount = 0;
        byte[] _keyboardColors;
        int _mouseLedCount = 0;
        byte[] _mouseColors;
        int[] _mbControllerLedCount;
        int[] _gpuControllerLedCount;
        Dictionary<int, byte[]> _mbColors;
        Dictionary<int, byte[]> _gpuColors;




        public bool Initialize()
        {
            Global.logger.Info("called initialize asus");
            if (!isInitialized)
            {
                Global.logger.Info("not itialized ");
                try
                {
                    Global.logger.Info("trying");
                    Aura = new AuraSdk();
                    Global.logger.Info("new sdk object");
                    if (!Aura.LoadDll())
                    {
                        Global.logger.Error("Asus: Failed to load DLL");
                        return false;
                    }
                    Global.logger.Info("maybe loaded");
                    _isKeyboardPresent = Aura.isKeyboardPresent();
                    _isMousePresent = Aura.isMousePresent();
                    _MbControllers = Aura.getMbAvailableControllers();
                    _GPUSControllers = Aura.getGPUAvailableControllers();


                    Global.logger.Info("Loaded Aura SDK, found:");
                    Global.logger.Info("Keyboard: " + _isKeyboardPresent);
                    Global.logger.Info("Mouse: " + _isMousePresent);
                    Global.logger.Info("Mb controllers: " + _MbControllers);
                    Global.logger.Info("GPU controllers: " + _GPUSControllers);

                    if (_isKeyboardPresent)
                    {
                        Global.logger.Info("Try to load keyboard");
                        _keyboardLedCount = Aura.GetKeyboardLedCount() * 3; // Need to mutiply for RGB
                        _keyboardColors = new byte[_keyboardLedCount];    // check if need to reinitialize when the numpad is disconnected
                        takeKeyboardControl();
                        Global.logger.Info("Found Asus Keyboard with: " + _keyboardLedCount + " Leds");
                    }

                    if (_isMousePresent)
                    {
                        Global.logger.Info("Try to load mouse");
                        _mouseLedCount = Aura.GetMouseLedCount() * 3; // Need to mutiply for RGB
                        _mouseColors = new byte[_mouseLedCount];
                        Global.logger.Info("Found Asus Mouse with: " + _keyboardLedCount + " Leds");
                    }

                    if (_MbControllers != 0)
                    {
                        Global.logger.Info("Try to load Mb");
                        _mbControllerLedCount = new int[_MbControllers];
                        for (int i = 0; i < _MbControllers; i++)
                        {
                            _mbControllerLedCount[i] = Aura.GetMBLedCount(i);
                            _mbColors = new Dictionary<int, byte[]>();
                            _mbColors.Add(i, new byte[_mbControllerLedCount[i] * 3]);
                            Global.logger.Info("Found Asus Mb controller id: " + i + " with: " + _mbControllerLedCount[i] + " Leds");
                        }
                    }

                    if (_GPUSControllers != 0)
                    {
                        Global.logger.Info("Try to load Gpu");
                        _gpuControllerLedCount = new int[_GPUSControllers];
                        for (int i = 0; i < _GPUSControllers; i++)
                        {
                            _gpuControllerLedCount[i] = Aura.GetGPUCtrlLedCount(i);
                            _gpuColors = new Dictionary<int, byte[]>();
                            _gpuColors.Add(i, new byte[_gpuControllerLedCount[i] * 3]);
                            Global.logger.Info("Found Asus GPU controller id: " + i + " with: " + _gpuControllerLedCount[i] + " Leds");
                        }
                    }


                    isInitialized = true;
                }
                catch (Exception e)
                {
                    Global.logger.Error("Can not load Asus DLL: " + e.ToString());
                    return false;
                }

            }
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
            throw new NotImplementedException();
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }

        public bool IsKeyboardConnected()
        {
            if (isInitialized)
            {
                return Aura.isKeyboardPresent();
            }
            return false;
        }

        public bool IsPeripheralConnected()
        {
            if (isInitialized)
            {
                return _isMousePresent || _MbControllers != 0 || _GPUSControllers != 0;
            }
            return false;
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
            isInitialized = false;
            dropPeripherealsControl();
            dropKeyboardControl();
            Aura.UnloadDll();
        }

        private void ApplyKeybordColor()
        {
            if (isInitialized)
            {
                Aura.SetKeyboardLedColor(_keyboardColors);

            }
        }

        private void SendColorToKeyboard(DeviceKeys key, Color color)
        {
            if (_keyboardInitialized)
            {
                _keyboardColors[KeyToClaymoreLedID(key)] = color.R; // RED
                _keyboardColors[KeyToClaymoreLedID(key) + 1] = color.G; // GREEN
                _keyboardColors[KeyToClaymoreLedID(key) + 2] = color.B;  // BLU
            }
        }

        private void takeKeyboardControl()
        {
            if (!_keyboardInitialized)
            {
                Aura.SetKeyboardLedMode(1);
                _keyboardInitialized = true;
            }
        }

        private void dropKeyboardControl()
        {
            if (_keyboardInitialized)
            {
                Aura.SetKeyboardLedMode(0);
                _keyboardInitialized = false;
            }

        }

        private void takeMbControl()
        {
            if (!_isMbInitialized)
            {
                for (var i = 0; i < _MbControllers; i++)
                {
                    Aura.SetMBLedMode(i, 1);
                    _isMbInitialized = true;
                }
            }
        }

        private void dropMbControl()
        {
            if (_isMbInitialized)
            {
                for (var i = 0; i < _MbControllers; i++)
                {
                    Aura.SetMBLedMode(i, 0);
                    _isMbInitialized = false;
                }
            }
        }


        private void takeGpuControl()
        {
            if (!_isGpuInitialized)
            {
                for (var i = 0; i < _GPUSControllers; i++)
                {
                    Aura.SetGPUCtrlLedMode(i, 1);
                    _isGpuInitialized = true;
                }
            }
        }

        private void dropGpuControl()
        {
            if (_isGpuInitialized)
            {
                for (var i = 0; i < _GPUSControllers; i++)
                {
                    Aura.SetGPUCtrlLedMode(i, 0);
                    _isGpuInitialized = false;
                }
            }
        }

        private void takeMouseControl()
        {
            if (_isMousePresent && !_isMouseInitialized)
            {
                Aura.SetMouseLedMode(1);
                _isMouseInitialized = true;
            }
        }

        private void dropMouseControl()
        {
            if (_isMousePresent && _isMouseInitialized)
            {
                Aura.SetMouseLedMode(0);
                _isMouseInitialized = false;
            }
        }

        private void dropPeripherealsControl()
        {
            dropMbControl();
            dropGpuControl();
            dropMouseControl();
        }

        private void SendColorToPeripheral(Color color)
        {

            if (_MbControllers != 0)
            {
                if (!_isMbInitialized)
                {
                    takeMbControl();
                }
                for (var i = 0; i < _MbControllers; i++)
                {
                    int ledCount = _mbControllerLedCount[i];
                    for (var k = 0; k < ledCount; k = k + 3)
                    {
                        _mbColors[i][k] = color.R;
                        _mbColors[i][k + 1] = color.G;
                        _mbColors[i][k + 2] = color.B;
                    }
                    Aura.SetMBLedColor(i, _mbColors[i]);
                }
            }

            if (_GPUSControllers != 0)
            {
                if (!_isGpuInitialized)
                {
                    takeGpuControl();
                }
                for (var i = 0; i < _GPUSControllers; i++)
                {
                    int ledCount = _gpuControllerLedCount[i];
                    for (var k = 0; k < ledCount; k = k + 3)
                    {
                        _gpuColors[i][k] = color.R;
                        _gpuColors[i][k + 1] = color.G;
                        _gpuColors[i][k + 2] = color.B;
                    }
                    Aura.SetGPUCtrlLedColor(i, _gpuColors[i]);
                }
            }

            if (_isMousePresent)
            {
                if (_isMouseInitialized)
                {
                    takeMouseControl();
                }
                for (var k = 0; k < _mouseLedCount; k = k + 3)
                {
                    _mouseColors[k] = color.R;
                    _mouseColors[k + 1] = color.G;
                    _mouseColors[k + 2] = color.B;
                }
                Aura.SetMouseLedColor(_mouseColors);
            }

        }

        public bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, CancellationToken token, bool forced = false)
        {
            if (token.IsCancellationRequested) return false;
            foreach (KeyValuePair<DeviceKeys, Color> key in keyColors)
            {
                if (token.IsCancellationRequested) return false;
                if (key.Key == DeviceKeys.Peripheral_Logo || key.Key == DeviceKeys.Peripheral)
                {
                    SendColorToPeripheral(key.Value);
                } else
                {
                    SendColorToKeyboard(key.Key, key.Value);
                }

            }

            if (token.IsCancellationRequested) return false;
            ApplyKeybordColor();
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
                    return 249;
                case DeviceKeys.PERIOD:
                    return 252;
                case DeviceKeys.FN_Key:
                    return 255;
                case DeviceKeys.MINUS:
                    return 267;
                case DeviceKeys.OPEN_BRACKET:
                    return 270;
                case DeviceKeys.APOSTROPHE:
                    return 273;
                case DeviceKeys.FORWARD_SLASH:
                    return 276;
                case DeviceKeys.APPLICATION_SELECT:
                    return 279;
                case DeviceKeys.F9:
                    return 288;
                case DeviceKeys.EQUALS:
                    return 291;
                case DeviceKeys.CLOSE_BRACKET:
                    return 294;
                case DeviceKeys.HASHTAG:
                    return 297;
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
