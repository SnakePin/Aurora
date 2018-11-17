﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Aurora.Settings;

using AuraServiceLib;
using Microsoft.Scripting.Utils;

namespace Aurora.Devices.Asus
{
    class AsusDevice : Device
    {
        private const string DeviceName = "Asus";

        private bool _isInitialized = false;
        private readonly object _lock = new object();

        private readonly List<AsusGenericHardwareDevice> _asusDevices = new List<AsusGenericHardwareDevice>();
        private AsusKeyboardHardwareDevice _keyboardDevice = null;

        #region Interface
        /// <inheritdoc />
        public VariableRegistry GetRegisteredVariables()
        {
            return new VariableRegistry();
        }

        /// <inheritdoc />
        public string GetDeviceName()
        {
            return DeviceName;
        }

        /// <inheritdoc />
        public string GetDeviceDetails()
        {
            return _asusDevices.Count > 0
                ? $"{DeviceName}: Connected devices {_asusDevices.Select(device => device.Details).Aggregate((d1, d2) => d1 + ", " + d2)}"
                : "{DeviceName}: No Connected devices";
        }

        /// <inheritdoc />
        public string GetDeviceUpdatePerformance()
        {
            return _asusDevices.Count > 0
                ? _asusDevices.Select(device => device.Status).Aggregate((d1, d2) => d1 + ", " + d2)
                : string.Empty;
        }

        /// <inheritdoc />
        [HandleProcessCorruptedStateExceptions]
        public bool Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized)
                    return true;

                Log("Initializing Device");
                _asusDevices.Clear();
                _keyboardDevice = null;

                try
                {
                    var asusDev = new AuraDevelopement();
                    asusDev.AURARequireToken();

                    var allDevices = asusDev.GetAllDevices();

                    Log($"Found {allDevices.Count} device(s)");
                    foreach (IAuraDevice auraDevice in allDevices)
                    {
                        Log($"Device -> {auraDevice.Name}, has {auraDevice.LightCount} LED lights");

                        // Due to lack of documentation I will make assumptions as to what these devices are
                        if (auraDevice is IAuraKeyboard keyboard)
                        {
                            _keyboardDevice = new AsusKeyboardHardwareDevice(new AuraSdkKeyboardWrapper(keyboard));
                            _asusDevices.Add(_keyboardDevice);
                        }
                        else if (auraDevice.Name.ToLower().Contains("mouse"))
                        {
                            _asusDevices.Add(new AsusMouseHardwareDevice(new AuraSdkDeviceWrapper(auraDevice)));
                        }
                        else if (auraDevice.Name.ToLower().Contains("vga"))
                        {
                            _asusDevices.Add(new AsusGenericHardwareDevice(new AuraSdkDeviceWrapper(auraDevice), "GPU"));
                        }
                        else
                        {
                            _asusDevices.Add(new AsusGenericHardwareDevice(new AuraSdkDeviceWrapper(auraDevice)));
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                _isInitialized = _asusDevices.Count > 0;
                return _isInitialized;
            }
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            _isInitialized = false;
        }

        /// <inheritdoc />
        public void Reset()
        {
//            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Reconnect()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool IsInitialized()
        {
            return _isInitialized;
        }

        /// <inheritdoc />
        public bool IsConnected()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool IsKeyboardConnected()
        {
            return _keyboardDevice != null;
        }

        /// <inheritdoc />
        public bool IsPeripheralConnected()
        {
            return _asusDevices.Count > 0;
        }
        
        /// <inheritdoc />
        [HandleProcessCorruptedStateExceptions]
        public bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, DoWorkEventArgs e, bool forced = false)
        {
            if (!_isInitialized || e.Cancel)
                return false;

            List<AsusGenericHardwareDevice> removedDevices = null;
            try
            {
                foreach (var device in _asusDevices)
                {
                    if (device.Connected)
                    {
                        device.SetColors(keyColors);
                    }
                    else
                    {
                        Log($"{device.Name} disconnected :(");
                        if (removedDevices == null)
                            removedDevices = new List<AsusGenericHardwareDevice>();
                        removedDevices.Add(device);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            // if any devices failed, the remove it from the list
            if (removedDevices != null)
            {
                foreach (var rDevice in removedDevices)
                {
                    _asusDevices.Remove(rDevice);
                    // if this device was a keyboard, then remove the reference as well
                    if (rDevice is AsusKeyboardHardwareDevice)
                        _keyboardDevice = null;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public bool UpdateDevice(DeviceColorComposition colorComposition, DoWorkEventArgs e, bool forced = false)
        {
            return UpdateDevice(colorComposition.keyColors, e, forced);
        }
        #endregion
        
        private static void SetDevicesKey(AuraSdkDeviceWrapper device, IReadOnlyDictionary<DeviceKeys, Color> keyColors, DeviceKeys key, int index)
        {
            if (keyColors.ContainsKey(key))
                SetRgbLight(device.Device.Lights[index], keyColors[key]);
        }

        private static void SetDevicesKeyIfExist(AuraSdkDeviceWrapper device, Func<DeviceKeys, int> keyMapper, IReadOnlyDictionary<DeviceKeys, Color> keyColors, DeviceKeys key)
        {
            if (keyColors.ContainsKey(key))
                SetRgbLight(device.Device.Lights[keyMapper(key)], keyColors[key]);
        }

        /// <summary>
        /// Sets an Aura RGB Light 
        /// </summary>
        /// <param name="rgbLight">The light to set</param>
        /// <param name="color">Color to set with</param>
        private static void SetRgbLight(IAuraRgbKey rgbLight, Color color)
        {
            rgbLight.Red = color.R;
            rgbLight.Green = color.G;
            rgbLight.Blue = color.B;
        }

        /// <summary>
        /// Sets an Aura RGB Light 
        /// </summary>
        /// <param name="rgbLight">The light to set</param>
        /// <param name="color">Color to set with</param>
        private static void SetRgbLight(IAuraRgbLight rgbLight, Color color)
        {
            rgbLight.Red = color.R;
            rgbLight.Green = color.G;
            rgbLight.Blue = color.B;
        }

        private static void Log(string logMessage)
        {
            Global.logger.Error($"ASUS device: {logMessage}");
        }

        #region Devices

        private class AsusKeyboardHardwareDevice : AsusGenericHardwareDevice
        {
            private readonly AuraSdkKeyboardWrapper _keyboard;

            /// <inheritdoc />
            public AsusKeyboardHardwareDevice(AuraSdkKeyboardWrapper keyboard) : base("Keyboard")
            {
                _keyboard = keyboard;
            }

            /// <inheritdoc />
            protected override void SetColorsAsync(Dictionary<DeviceKeys, Color> keyColors)
            {
                try
                {
                    // set to `individual key` mode
                    _keyboard.Device.SetMode(0);

                    // set keyboard colors
                    foreach (var keyPair in keyColors)
                        SetKeyboardColor(keyPair.Key, keyPair.Value);

                    // send LED data to keyboard
                    _keyboard.Device.Apply();
                }
                catch
                {
                    Connected = false;
                }
            }

            private void SetKeyboardColor(DeviceKeys deviceKey, Color color)
            {
                var keyId = DeviceKeyToAuraKeyboardKeyId(deviceKey);
                // one quick check
                if (_keyboard == null)
                    return;

                // if key is invalid
                if (keyId == ushort.MaxValue || !_keyboard.IdToKey.ContainsKey(keyId))
                    return;

                var key = _keyboard.IdToKey[keyId];
                SetRgbLight(key, color);
            }

            /// <summary>
            /// Determines the ushort ID from a DeviceKeys
            /// </summary>
            /// <param name="key">The key to translate</param>
            /// <returns>the ushort id, or ushort.MaxValue if invalid</returns>
            private static ushort DeviceKeyToAuraKeyboardKeyId(DeviceKeys key)
            {
                switch (key)
                {
                    case DeviceKeys.ESC:
                        return 1;
                    case DeviceKeys.F1:
                        return 59;
                    case DeviceKeys.F2:
                        return 60;
                    case DeviceKeys.F3:
                        return 61;
                    case DeviceKeys.F4:
                        return 62;
                    case DeviceKeys.F5:
                        return 63;
                    case DeviceKeys.F6:
                        return 64;
                    case DeviceKeys.F7:
                        return 65;
                    case DeviceKeys.F8:
                        return 66;
                    case DeviceKeys.F9:
                        return 67;
                    case DeviceKeys.F10:
                        return 68;
                    case DeviceKeys.F11:
                        return 87;
                    case DeviceKeys.F12:
                        return 88;
                    case DeviceKeys.PRINT_SCREEN:
                        return 183;
                    case DeviceKeys.SCROLL_LOCK:
                        return 70;
                    case DeviceKeys.PAUSE_BREAK:
                        return 197;
                    case DeviceKeys.OEM5:
                        return 6;
                    case DeviceKeys.TILDE:
                        return 41;
                    case DeviceKeys.ONE:
                        return 2;
                    case DeviceKeys.TWO:
                        return 3;
                    case DeviceKeys.THREE:
                        return 4;
                    case DeviceKeys.FOUR:
                        return 5;
                    case DeviceKeys.FIVE:
                        return 6;
                    case DeviceKeys.SIX:
                        return 7;
                    case DeviceKeys.SEVEN:
                        return 8;
                    case DeviceKeys.EIGHT:
                        return 9;
                    case DeviceKeys.NINE:
                        return 10;
                    case DeviceKeys.ZERO:
                        return 11;
                    case DeviceKeys.MINUS:
                        return 12;
                    case DeviceKeys.EQUALS:
                        return 13;
                    case DeviceKeys.OEM6:
                        return 7;
                    case DeviceKeys.BACKSPACE:
                        return 14;
                    case DeviceKeys.INSERT:
                        return 210;
                    case DeviceKeys.HOME:
                        return 199;
                    case DeviceKeys.PAGE_UP:
                        return 201;
                    case DeviceKeys.NUM_LOCK:
                        return 69;
                    case DeviceKeys.NUM_SLASH:
                        return 181;
                    case DeviceKeys.NUM_ASTERISK:
                        return 55;
                    case DeviceKeys.NUM_MINUS:
                        return 74;
                    case DeviceKeys.TAB:
                        return 15;
                    case DeviceKeys.Q:
                        return 16;
                    case DeviceKeys.W:
                        return 17;
                    case DeviceKeys.E:
                        return 18;
                    case DeviceKeys.R:
                        return 19;
                    case DeviceKeys.T:
                        return 20;
                    case DeviceKeys.Y:
                        return 21;
                    case DeviceKeys.U:
                        return 22;
                    case DeviceKeys.I:
                        return 23;
                    case DeviceKeys.O:
                        return 24;
                    case DeviceKeys.P:
                        return 25;
                    case DeviceKeys.OEM1:
                        return 2;
                    case DeviceKeys.OPEN_BRACKET:
                        return 26;
                    case DeviceKeys.OEMPlus:
                        return 13;
                    case DeviceKeys.CLOSE_BRACKET:
                        return 27;
                    case DeviceKeys.BACKSLASH:
                        return 43;
                    case DeviceKeys.DELETE:
                        return 211;
                    case DeviceKeys.END:
                        return 207;
                    case DeviceKeys.PAGE_DOWN:
                        return 209;
                    case DeviceKeys.NUM_SEVEN:
                        return 71;
                    case DeviceKeys.NUM_EIGHT:
                        return 72;
                    case DeviceKeys.NUM_NINE:
                        return 73;
                    case DeviceKeys.NUM_PLUS:
                        return 78;
                    case DeviceKeys.CAPS_LOCK:
                        return 58;
                    case DeviceKeys.A:
                        return 30;
                    case DeviceKeys.S:
                        return 31;
                    case DeviceKeys.D:
                        return 32;
                    case DeviceKeys.F:
                        return 33;
                    case DeviceKeys.G:
                        return 34;
                    case DeviceKeys.H:
                        return 35;
                    case DeviceKeys.J:
                        return 36;
                    case DeviceKeys.K:
                        return 37;
                    case DeviceKeys.L:
                        return 38;
                    case DeviceKeys.OEMTilde:
                        return 41;
                    case DeviceKeys.SEMICOLON:
                        return 39;
                    case DeviceKeys.APOSTROPHE:
                        return 40;
                    case DeviceKeys.HASHTAG:
                        return 3;
                    case DeviceKeys.ENTER:
                        return 28;
                    case DeviceKeys.NUM_FOUR:
                        return 75;
                    case DeviceKeys.NUM_FIVE:
                        return 76;
                    case DeviceKeys.NUM_SIX:
                        return 77;
                    case DeviceKeys.LEFT_SHIFT:
                        return 42;
                    case DeviceKeys.BACKSLASH_UK:
                        return 43;
                    case DeviceKeys.Z:
                        return 44;
                    case DeviceKeys.X:
                        return 45;
                    case DeviceKeys.C:
                        return 46;
                    case DeviceKeys.V:
                        return 47;
                    case DeviceKeys.B:
                        return 48;
                    case DeviceKeys.N:
                        return 49;
                    case DeviceKeys.M:
                        return 50;
                    case DeviceKeys.COMMA:
                        return 51;
                    case DeviceKeys.PERIOD:
                        return 52;
                    case DeviceKeys.FORWARD_SLASH:
                        return 53;
                    case DeviceKeys.OEM8:
                        return 9;
                    case DeviceKeys.RIGHT_SHIFT:
                        return 54;
                    case DeviceKeys.ARROW_UP:
                        return 200;
                    case DeviceKeys.NUM_ONE:
                        return 79;
                    case DeviceKeys.NUM_TWO:
                        return 80;
                    case DeviceKeys.NUM_THREE:
                        return 81;
                    case DeviceKeys.NUM_ENTER:
                        return 156;
                    case DeviceKeys.LEFT_CONTROL:
                        return 29;
                    case DeviceKeys.LEFT_WINDOWS:
                        return 219;
                    case DeviceKeys.LEFT_ALT:
                        return 56;
                    case DeviceKeys.SPACE:
                        return 57;
                    case DeviceKeys.RIGHT_ALT:
                        return 184;
                    case DeviceKeys.APPLICATION_SELECT:
                        return 221;
                    case DeviceKeys.RIGHT_CONTROL:
                        return 157;
                    case DeviceKeys.ARROW_LEFT:
                        return 203;
                    case DeviceKeys.ARROW_DOWN:
                        return 208;
                    case DeviceKeys.ARROW_RIGHT:
                        return 205;
                    case DeviceKeys.NUM_ZERO:
                        return 82;
                    case DeviceKeys.NUM_PERIOD:
                        return 83;
                    case DeviceKeys.FN_Key:
                        return 256;
                    case DeviceKeys.LOGO:
                        return 257;
                    case DeviceKeys.ADDITIONALLIGHT1:
                        // LEFT OF STRIX FLARE KEYBOARD
                        return 258;
                    case DeviceKeys.ADDITIONALLIGHT2:
                        //RIGHT OF STRIX FLARE KEYBOARD
                        return 259;
                    default:
                        return ushort.MaxValue;
                }
            }

        }

        private class AsusMouseHardwareDevice : AsusGenericHardwareDevice
        {
            /// <inheritdoc />
            public AsusMouseHardwareDevice(AuraSdkDeviceWrapper mouseWrapper) : base(mouseWrapper, "Mouse")
            {
            }

            /// <inheritdoc />
            protected override void SetColorsAsync(Dictionary<DeviceKeys, Color> keyColors)
            {
                try
                {
                    // 0 Should be manual mode
                    _deviceWrapper.Device.SetMode(0);

                    // I'm not sure how to distinguish mice, so if it has 3 LED lights im going to assume it's a Pugio
                    if (_deviceWrapper.LightCount == 3)
                        SetPugioMouseColors(keyColors);
                    else
                        SetGenericMouseColors(keyColors);
                    
                    _deviceWrapper.Device.Apply();
                }
                catch
                {
                    Connected = false;
                }
            }

            private void SetPugioMouseColors(IReadOnlyDictionary<DeviceKeys, Color> keyColors)
            {
                // access keys directly since we know what we want
                SetDevicesKeyIfExist(_deviceWrapper, DeviceKeyToAuraPugioMouseKeyId, keyColors, DeviceKeys.Peripheral_Logo);
                SetDevicesKeyIfExist(_deviceWrapper, DeviceKeyToAuraPugioMouseKeyId, keyColors, DeviceKeys.Peripheral_ScrollWheel);
                SetDevicesKeyIfExist(_deviceWrapper, DeviceKeyToAuraPugioMouseKeyId, keyColors, DeviceKeys.Peripheral_FrontLight);
            }

            private void SetGenericMouseColors(Dictionary<DeviceKeys, Color> keyColors)
            {
                base.SetColorsAsync(keyColors);
            }
            
            /// <summary>
            /// Determines the ushort ID from a DeviceKeys
            /// </summary>
            /// <param name="key">The key to translate</param>
            /// <returns>the index of that mouse LED</returns>
            private static int DeviceKeyToAuraPugioMouseKeyId(DeviceKeys key)
            {
                switch (key)
                {
                    case DeviceKeys.Peripheral_Logo:
                        return 0;
                    case DeviceKeys.Peripheral_ScrollWheel:
                        return 1;
                    case DeviceKeys.Peripheral_FrontLight:
                        return 2;
                    default:
                        return ushort.MaxValue;
                }
            }
        }

        private class AsusGenericHardwareDevice
        {
            private Task _task = Task.Factory.StartNew(() => { });
            protected readonly AuraSdkDeviceWrapper _deviceWrapper;
            private readonly System.Diagnostics.Stopwatch _watch = new System.Diagnostics.Stopwatch();
            public readonly string Name;
            private long _elapsedTime = 0;

            public bool Connected {get; protected set; }
            public string Details => Name;
            public string Status => $"{Name} {_elapsedTime}ms";

            protected AsusGenericHardwareDevice(string name)
            {
                _deviceWrapper = null;
                Name = name;
                Connected = true;
            }

            public AsusGenericHardwareDevice(AuraSdkDeviceWrapper deviceWrapper, string name = null)
            {
                _deviceWrapper = deviceWrapper;
                Name = name ?? deviceWrapper.Device.Name;
                Connected = true;
            }

            /// <summary>
            /// Set the colors on the peripheral device
            /// </summary>
            /// <param name="keyColors">The colors this frame</param>
            public void SetColors(Dictionary<DeviceKeys, Color> keyColors)
            {
                // If a task is running, don't set the colors, wait for the next set
                if (!_task.IsCompleted)
                    return;

                _task = Task.Factory.StartNew(() =>
                {
                    _watch.Restart();
                    SetColorsAsync(keyColors);
                    _watch.Stop();
                    _elapsedTime = _watch.ElapsedMilliseconds;
                });
            }

            /// <summary>
            /// Set the colors asynchronously, default behaviour is to set all colors DeviceKeys.Peripheral_Logo
            /// </summary>
            /// <param name="keyColors">The colors this frame</param>
            protected virtual void SetColorsAsync(Dictionary<DeviceKeys, Color> keyColors)
            {
                try
                {
                    // 0 Should be manual mode
                    _deviceWrapper.Device.SetMode(0);

                    // set all LEDs to DeviceKeys.Peripheral
                    for (int i = 0; i < _deviceWrapper.LightCount; i++)
                        SetDevicesKey(_deviceWrapper, keyColors, DeviceKeys.Peripheral_Logo, i);

                    _deviceWrapper.Device.Apply();
                }
                catch
                {
                    Connected = false;
                }
            }
        }

        #endregion

        #region Wrappers
        /// <summary>
        /// A simple wrapper class to make things a bit neater
        /// </summary>
        private class AuraSdkDeviceWrapper : IAuraDevice
        {
            public IAuraDevice Device { get; private set; }

            public AuraSdkDeviceWrapper(IAuraDevice device)
            {
                Device = device;
            }
            
            /// <inheritdoc />
            void IAuraSyncDevice.Apply()
            {
                ((IAuraSyncDevice) Device).Apply();
            }
            
            /// <inheritdoc />
            IAuraRgbLightCollection IAuraDevice.Lights => Device.Lights;

            /// <inheritdoc />
            uint IAuraDevice.Type => Device.Type;

            /// <inheritdoc />
            string IAuraDevice.Name => Device.Name;

            /// <inheritdoc />
            uint IAuraDevice.Width => Device.Width;

            /// <inheritdoc />
            uint IAuraDevice.Height => Device.Height;

            /// <inheritdoc />
            public int LightCount
            {
                get => Device.LightCount;
                set => Device.LightCount = value;
            }

            /// <inheritdoc />
            public IAuraEffectCollection Effects => Device.Effects;

            /// <inheritdoc />
            public IAuraEffectCollection StandbyEffects => Device.StandbyEffects;

            /// <inheritdoc />
            public int DefaultLightCount => Device.DefaultLightCount;

            /// <inheritdoc />
            public int MaxLightCount => Device.MaxLightCount;

            /// <inheritdoc />
            public uint LightCountVariable => Device.LightCountVariable;

            /// <inheritdoc />
            public string Manufacture => Device.Manufacture;

            /// <inheritdoc />
            public string Model => Device.Model;

            /// <inheritdoc />
            public IAuraRgbLightGroupCollection Groups => Device.Groups;

            /// <inheritdoc />
            public void SetMode(int mode)
            {
                Device.SetMode(mode);
            }

            /// <inheritdoc />
            public void SetLightColor(uint index, uint Color)
            {
                Device.SetLightColor(index, Color);
            }

            /// <inheritdoc />
            public void GetLayout(out uint Width, out uint Height, out uint depth)
            {
                Device.GetLayout(out Width, out Height, out depth);
            }

            /// <inheritdoc />
            public void Synchronize(uint effectIndex, uint tickcount)
            {
                Device.Synchronize(effectIndex, tickcount);
            }

            /// <inheritdoc />
            void IAuraDevice.Apply()
            {
                Device.Apply();
            }

            /// <inheritdoc />
            IAuraRgbLightCollection IAuraSyncDevice.Lights => ((IAuraSyncDevice) Device).Lights;

            /// <inheritdoc />
            uint IAuraSyncDevice.Type => ((IAuraSyncDevice) Device).Type;

            /// <inheritdoc />
            string IAuraSyncDevice.Name => ((IAuraSyncDevice) Device).Name;

            /// <inheritdoc />
            uint IAuraSyncDevice.Width => ((IAuraSyncDevice) Device).Width;

            /// <inheritdoc />
            uint IAuraSyncDevice.Height => ((IAuraSyncDevice) Device).Height;
        }

        /// <summary>
        /// A simple wrapper class to make things a bit neater
        /// </summary>
        private class AuraSdkKeyboardWrapper : IAuraKeyboard
        {
            private readonly IAuraKeyboard _device;
            public IAuraDevice Device => _device;
            public AuraSdkKeyboardWrapper(IAuraKeyboard device)
            {
                _device = device;
                Initialize();
            }

            private void Initialize()
            {
                foreach (IAuraRgbKey key in Keys)
                {
                    IdToKey[key.Code] = key;
                }
            }

            public readonly Dictionary<ushort, IAuraRgbKey> IdToKey 
                = new Dictionary<ushort, IAuraRgbKey>();

            /// <inheritdoc />
            void IAuraSyncDevice.Apply()
            {
                ((IAuraSyncDevice) _device).Apply();
            }

            /// <inheritdoc />
            IAuraRgbLightCollection IAuraKeyboard.Lights => _device.Lights;

            /// <inheritdoc />
            uint IAuraKeyboard.Type => _device.Type;

            /// <inheritdoc />
            string IAuraKeyboard.Name => _device.Name;

            /// <inheritdoc />
            uint IAuraKeyboard.Width => _device.Width;

            /// <inheritdoc />
            uint IAuraKeyboard.Height => _device.Height;

            /// <inheritdoc />
            int IAuraKeyboard.LightCount
            {
                get => _device.LightCount;
                set => _device.LightCount = value;
            }

            /// <inheritdoc />
            void IAuraKeyboard.SetMode(int mode)
            {
                _device.SetMode(mode);
            }

            /// <inheritdoc />
            void IAuraKeyboard.SetLightColor(uint index, uint Color)
            {
                _device.SetLightColor(index, Color);
            }

            /// <inheritdoc />
            void IAuraKeyboard.GetLayout(out uint Width, out uint Height, out uint depth)
            {
                _device.GetLayout(out Width, out Height, out depth);
            }

            /// <inheritdoc />
            void IAuraKeyboard.Synchronize(uint effectIndex, uint tickcount)
            {
                _device.Synchronize(effectIndex, tickcount);
            }

            /// <inheritdoc />
            public IAuraRgbKeyStateCollection WaitKeyInput(IntPtr @event, uint timeout)
            {
                return _device.WaitKeyInput(@event, timeout);
            }

            /// <inheritdoc />
            void IAuraKeyboard.Apply()
            {
                _device.Apply();
            }

            /// <inheritdoc />
            IAuraRgbLightCollection IAuraDevice.Lights => ((IAuraDevice) _device).Lights;

            /// <inheritdoc />
            uint IAuraDevice.Type => ((IAuraDevice) _device).Type;

            /// <inheritdoc />
            string IAuraDevice.Name => ((IAuraDevice) _device).Name;

            /// <inheritdoc />
            uint IAuraDevice.Width => ((IAuraDevice) _device).Width;

            /// <inheritdoc />
            uint IAuraDevice.Height => ((IAuraDevice) _device).Height;

            /// <inheritdoc />
            int IAuraDevice.LightCount
            {
                get => ((IAuraDevice) _device).LightCount;
                set => ((IAuraDevice) _device).LightCount = value;
            }

            /// <inheritdoc />
            IAuraEffectCollection IAuraDevice.Effects => ((IAuraDevice) _device).Effects;

            /// <inheritdoc />
            IAuraEffectCollection IAuraKeyboard.StandbyEffects => _device.StandbyEffects;

            /// <inheritdoc />
            int IAuraKeyboard.DefaultLightCount => _device.DefaultLightCount;

            /// <inheritdoc />
            int IAuraKeyboard.MaxLightCount => _device.MaxLightCount;

            /// <inheritdoc />
            uint IAuraKeyboard.LightCountVariable => _device.LightCountVariable;

            /// <inheritdoc />
            string IAuraKeyboard.Manufacture => _device.Manufacture;

            /// <inheritdoc />
            string IAuraKeyboard.Model => _device.Model;

            /// <inheritdoc />
            IAuraRgbLightGroupCollection IAuraKeyboard.Groups => _device.Groups;

            /// <inheritdoc />
            public IAuraRgbLight get_Key(ushort keyCode)
            {
                return _device.get_Key(keyCode);
            }

            /// <inheritdoc />
            public IAuraRgbKeyCollection Keys => _device.Keys;

            /// <inheritdoc />
            IAuraEffectCollection IAuraKeyboard.Effects => _device.Effects;

            /// <inheritdoc />
            IAuraEffectCollection IAuraDevice.StandbyEffects => ((IAuraDevice) _device).StandbyEffects;

            /// <inheritdoc />
            int IAuraDevice.DefaultLightCount => ((IAuraDevice) _device).DefaultLightCount;

            /// <inheritdoc />
            int IAuraDevice.MaxLightCount => ((IAuraDevice) _device).MaxLightCount;

            /// <inheritdoc />
            uint IAuraDevice.LightCountVariable => ((IAuraDevice) _device).LightCountVariable;

            /// <inheritdoc />
            string IAuraDevice.Manufacture => ((IAuraDevice) _device).Manufacture;

            /// <inheritdoc />
            string IAuraDevice.Model => ((IAuraDevice) _device).Model;

            /// <inheritdoc />
            IAuraRgbLightGroupCollection IAuraDevice.Groups => ((IAuraDevice) _device).Groups;

            /// <inheritdoc />
            void IAuraDevice.SetMode(int mode)
            {
                ((IAuraDevice) _device).SetMode(mode);
            }

            /// <inheritdoc />
            void IAuraDevice.SetLightColor(uint index, uint Color)
            {
                ((IAuraDevice) _device).SetLightColor(index, Color);
            }

            /// <inheritdoc />
            void IAuraDevice.GetLayout(out uint Width, out uint Height, out uint depth)
            {
                ((IAuraDevice) _device).GetLayout(out Width, out Height, out depth);
            }

            /// <inheritdoc />
            void IAuraDevice.Synchronize(uint effectIndex, uint tickcount)
            {
                ((IAuraDevice) _device).Synchronize(effectIndex, tickcount);
            }

            /// <inheritdoc />
            void IAuraDevice.Apply()
            {
                ((IAuraDevice) _device).Apply();
            }

            /// <inheritdoc />
            IAuraRgbLightCollection IAuraSyncDevice.Lights => ((IAuraSyncDevice) _device).Lights;

            /// <inheritdoc />
            uint IAuraSyncDevice.Type => ((IAuraSyncDevice) _device).Type;

            /// <inheritdoc />
            string IAuraSyncDevice.Name => ((IAuraSyncDevice) _device).Name;

            /// <inheritdoc />
            uint IAuraSyncDevice.Width => ((IAuraSyncDevice) _device).Width;

            /// <inheritdoc />
            uint IAuraSyncDevice.Height => ((IAuraSyncDevice) _device).Height;
        }
        #endregion
    }
}
