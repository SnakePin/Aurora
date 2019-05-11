﻿using Aurora.Settings;
using HidLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aurora.Devices.RoccatVulcan
{
    class RoccatVulcan : Device
    {
        private String devicename = "Roccat Vulcan";
        private bool isInitialized = false;

        private bool keyboard_updated = false;
        private VariableRegistry default_registry = null;

        private readonly object action_lock = new object();

        private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private long lastUpdateTime = 0;


        [DllImport("kernel32.dll")]
        static internal extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, [In] ref System.Threading.NativeOverlapped lpOverlapped);

        private static HidDevice ctrl_device_leds;
        private static HidDevice ctrl_device;
        private static int RV_NUM_KEYS = 144;
        private struct rv_rgb
        {
            public byte r;
            public byte g;
            public byte b;
        }
        private class rv_rgb_map
        {
            public rv_rgb_map()
            {
                keys = new rv_rgb[RV_NUM_KEYS];
            }

            public rv_rgb[] keys;
        }

        public bool Initialize()
        {
            lock (action_lock)
            {
                if (!isInitialized)
                {
                    try
                    {
                        IEnumerable<HidDevice> devices = HidDevices.Enumerate(0x1E7D, new int[] { 0x307A, 0x3098 });

                        if (devices.Count() > 0)
                        {
                            HidDevice[] devicearray = devices.ToArray();
                            ctrl_device_leds = devices.FirstOrDefault(dev => dev.Capabilities.UsagePage == 0x0001 && dev.Capabilities.Usage == 0x0000);
                            ctrl_device = devices.FirstOrDefault(dev => dev.Capabilities.FeatureReportByteLength > 50);

                            ctrl_device.OpenDevice();
                            ctrl_device_leds.OpenDevice();


                            bool success =
                        rv_get_ctrl_report(0x0f) &&
                        rv_set_ctrl_report(0x15) &&
                        rv_wait_for_ctrl_device() &&
                        rv_set_ctrl_report(0x05) &&
                        rv_wait_for_ctrl_device() &&
                        rv_set_ctrl_report(0x07) &&
                        rv_wait_for_ctrl_device() &&
                        rv_set_ctrl_report(0x0a) &&
                        rv_wait_for_ctrl_device() &&
                        rv_set_ctrl_report(0x0b) &&
                        rv_wait_for_ctrl_device() &&
                        rv_set_ctrl_report(0x06) &&
                        rv_wait_for_ctrl_device() &&
                        rv_set_ctrl_report(0x09) &&
                        rv_wait_for_ctrl_device() &&
                        rv_set_ctrl_report(0x0d) &&
                        rv_wait_for_ctrl_device() &&
                        rv_set_ctrl_report(0x13) &&
                        rv_wait_for_ctrl_device();


                            isInitialized = true;
                        }
                    }
                    catch (Exception exc)
                    {
                        Global.logger.Error("There was an error initializing Roccat Vulcan.\r\n" + exc.Message);

                        return false;
                    }
                }

                if (!isInitialized)
                    Global.logger.Info("No Roccat Vuşcan devices successfully Initialized!");

                return isInitialized;
            }
        }

        ~RoccatVulcan()
        {
            this.Shutdown();
        }

        public void Shutdown()
        {
            lock (action_lock)
            {
                if (isInitialized)
                {
                    ctrl_device.CloseDevice();
                    ctrl_device_leds.CloseDevice();
                    isInitialized = false;
                }
            }
        }

        public string GetDeviceDetails()
        {
            if (isInitialized)
            {
                string devString = devicename + ": ";
                devString += "Connected";
                return devString;
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

        public void Reset()
        {
            if (this.IsInitialized() && keyboard_updated)
            {
                keyboard_updated = false;
            }
        }

        public bool Reconnect()
        {
            throw new NotImplementedException();
        }

        public bool IsConnected()
        {
            throw new NotImplementedException();
        }

        public bool IsInitialized()
        {
            return this.isInitialized;
        }

        public bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, DoWorkEventArgs e, bool forced = false)
        {
            try
            {
                //Do this to prevent setting lighting again after the keyboard has been shutdown and reset
                lock (action_lock)
                {
                    if (!this.isInitialized)
                        return false;

                    rv_rgb_map rgbMap = new rv_rgb_map();

                    foreach (KeyValuePair<DeviceKeys, Color> key in keyColors)
                    {
                        if (e.Cancel) return false;


                        Color clr = Color.FromArgb(255, Utils.ColorUtils.MultiplyColorByScalar(key.Value, key.Value.A / 255.0D));

                        int index = DeviceKeyToRoccatVulcanIndex(key.Key);

                        if (index == -1)
                        {
                            continue;
                        }

                        rgbMap.keys[index].r = clr.R;
                        rgbMap.keys[index].g = clr.G;
                        rgbMap.keys[index].b = clr.B;

                    }
                    if (e.Cancel) return false;

                    rv_send_led_map(rgbMap);
                }
                return true;
            }
            catch (Exception exc)
            {
                Global.logger.Error("Failed to Update Device" + exc.ToString());
                return false;
            }
        }

        public bool UpdateDevice(DeviceColorComposition colorComposition, DoWorkEventArgs e, bool forced = false)
        {
            watch.Restart();

            bool update_result = UpdateDevice(colorComposition.keyColors, e, forced);

            watch.Stop();
            lastUpdateTime = watch.ElapsedMilliseconds;

            return update_result;
        }

        public bool IsKeyboardConnected()
        {
            return isInitialized;
        }

        public bool IsPeripheralConnected()
        {
            return isInitialized;
        }

        public string GetDeviceUpdatePerformance()
        {
            return (isInitialized ? lastUpdateTime + " ms" : "");
        }

        public VariableRegistry GetRegisteredVariables()
        {
            if (default_registry == null)
            {
                default_registry = new VariableRegistry();
                default_registry.Register($"{devicename}_scalar_r", 100, "Red Scalar", 100, 0);
                default_registry.Register($"{devicename}_scalar_g", 100, "Green Scalar", 100, 0);
                default_registry.Register($"{devicename}_scalar_b", 100, "Blue Scalar", 100, 0, "In percent");
            }
            return default_registry;
        }

        public static Dictionary<DeviceKeys, int> KeyMap = new Dictionary<DeviceKeys, int> {
            { DeviceKeys.ESC, 0 },
            { DeviceKeys.TILDE, 1 },
            { DeviceKeys.TAB, 2 },
            { DeviceKeys.LEFT_SHIFT, 3 },
            { DeviceKeys.LEFT_CONTROL, 4 },

            { DeviceKeys.Q, 5 },
            { DeviceKeys.A, 6 },
            { DeviceKeys.Z, 7 },
        };

        public static int DeviceKeyToRoccatVulcanIndex(DeviceKeys key)
        {
            if (KeyMap.TryGetValue(key, out int rv_key))
                return rv_key;

            return -1;
        }

        static bool rv_send_led_map(rv_rgb_map src)
        {
            // Send seven chunks with 64 bytes each
            byte[] hwmap = new byte[444];
            // Plus one byte report ID for the lib
            byte[] workbuf = new byte[65];

            // Translate linear to hardware map
            for (int k = 0; k < RV_NUM_KEYS; k++)
            {
                if (src == null || src.keys == null)
                {
                    continue;
                }

                int offset = ((k / 12) * 36) + (k % 12);

                hwmap[offset + 0] = src.keys[k].r;
                hwmap[offset + 12] = src.keys[k].g;
                hwmap[offset + 24] = src.keys[k].b;

            }

            // First chunk comes with header
            workbuf[0] = 0x00;
            workbuf[1] = 0xa1;
            workbuf[2] = 0x01;
            workbuf[3] = 0x01;
            workbuf[4] = 0xb4;

            //memcpy(&workbuf[5], hwmap, 60);
            Array.Copy(hwmap, 0, workbuf, 5, 60);

            NativeOverlapped overlapped = new NativeOverlapped();

            if (WriteFile(ctrl_device_leds.Handle, workbuf, (uint)workbuf.Length, out _, ref overlapped) != true)
            {
                return false;
            }

            // Six more chunks
            for (int i = 1; i < 7; i++)
            {
                workbuf[0] = 0x00;

                //memcpy(&workbuf[1], &hwmap[(i * 64) - 4], 64);
                Array.Copy(hwmap, (i * 64) - 4, workbuf, 1, 64);

                if (WriteFile(ctrl_device_leds.Handle, workbuf, (uint)workbuf.Length, out _, ref overlapped) != true)
                {
                    return false;
                }
            }

            return true;
        }

        static bool rv_wait_for_ctrl_device()
        {
            for (int i = 1; i < 100; i++) // If still fails after 100 tries then timeout
            {
                // 150ms is the magic number here, should suffice on first try.
                Thread.Sleep(150);
                bool success = ctrl_device.ReadFeatureData(out byte[] buffer, 0x04);
                if (success && buffer.Length > 2)
                {
                    if (buffer[1] == 0x01)
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        private bool rv_get_ctrl_report(byte report_id)
        {
            bool success = ctrl_device.ReadFeatureData(out byte[] buffer, report_id);

            return (success && buffer.Length >= 8);
        }

        private bool rv_set_ctrl_report(byte report_id)
        {
            byte[] buffer = null;
            int length = 0;

            switch (report_id)
            {
                case 0x15:
                    buffer = new byte[] { 0x15, 0x00, 0x01 };
                    length = 3;
                    break;
                case 0x05:
                    buffer = new byte[] { 0x05, 0x04, 0x00, 0x04 };
                    length = 4;
                    break;
                case 0x07:
                    buffer = new byte[] { 0x07 ,0x5f ,0x00 ,0x3a ,0x00 ,0x00 ,0x3b ,0x00 ,0x00 ,0x3c ,0x00 ,0x00 ,0x3d ,0x00 ,0x00 ,0x3e,
                        0x00 ,0x00 ,0x3f ,0x00 ,0x00 ,0x40 ,0x00 ,0x00 ,0x41 ,0x00 ,0x00 ,0x42 ,0x00 ,0x00 ,0x43 ,0x00,
                        0x00 ,0x44 ,0x00 ,0x00 ,0x45 ,0x00 ,0x00 ,0x46 ,0x00 ,0x00 ,0x47 ,0x00 ,0x00 ,0x48 ,0x00 ,0x00,
                        0xb3 ,0x00 ,0x00 ,0xb4 ,0x00 ,0x00 ,0xb5 ,0x00 ,0x00 ,0xb6 ,0x00 ,0x00 ,0xc2 ,0x00 ,0x00 ,0xc3,
                        0x00 ,0x00 ,0xc0 ,0x00 ,0x00 ,0xc1 ,0x00 ,0x00 ,0xce ,0x00 ,0x00 ,0xcf ,0x00 ,0x00 ,0xcc ,0x00,
                        0x00 ,0xcd ,0x00 ,0x00 ,0x46 ,0x00 ,0x00 ,0xfc ,0x00 ,0x00 ,0x48 ,0x00 ,0x00 ,0xcd ,0x0e };
                    length = 95;
                    break;
                case 0x0a:
                    buffer = new byte[] { 0x0a, 0x08, 0x00, 0xff, 0xf1, 0x00, 0x02, 0x02 };
                    length = 8;
                    break;
                case 0x0b:
                    buffer = new byte[] { 0x0b ,0x41 ,0x00 ,0x1e ,0x00 ,0x00 ,0x1f ,0x00 ,0x00 ,0x20 ,0x00 ,0x00 ,0x21 ,0x00 ,0x00 ,0x22,
                        0x00 ,0x00 ,0x14 ,0x00 ,0x00 ,0x1a ,0x00 ,0x00 ,0x08 ,0x00 ,0x00 ,0x15 ,0x00 ,0x00 ,0x17 ,0x00,
                        0x00 ,0x04 ,0x00 ,0x00 ,0x16 ,0x00 ,0x00 ,0x07 ,0x00 ,0x00 ,0x09 ,0x00 ,0x00 ,0x0a ,0x00 ,0x00,
                        0x1d ,0x00 ,0x00 ,0x1b ,0x00 ,0x00 ,0x06 ,0x00 ,0x00 ,0x19 ,0x00 ,0x00 ,0x05 ,0x00 ,0x00 ,0xde ,0x01};
                    length = 65;
                    break;
                case 0x06:
                    buffer = new byte[] { 0x06 ,0x85 ,0x00 ,0x3a ,0x29 ,0x35 ,0x1e ,0x2b ,0x39 ,0xe1 ,0xe0 ,0x3b ,0x1f ,0x14 ,0x1a ,0x04,
                        0x64 ,0x00 ,0x00 ,0x3d ,0x3c ,0x20 ,0x21 ,0x08 ,0x16 ,0x1d ,0xe2 ,0x3e ,0x23 ,0x22 ,0x15 ,0x07,
                        0x1b ,0x06 ,0x8b ,0x3f ,0x24 ,0x00 ,0x17 ,0x0a ,0x09 ,0x19 ,0x91 ,0x40 ,0x41 ,0x00 ,0x1c ,0x18,
                        0x0b ,0x05 ,0x2c ,0x42 ,0x26 ,0x25 ,0x0c ,0x0d ,0x0e ,0x10 ,0x11 ,0x43 ,0x2a ,0x27 ,0x2d ,0x12,
                        0x0f ,0x36 ,0x8a ,0x44 ,0x45 ,0x89 ,0x2e ,0x13 ,0x33 ,0x37 ,0x90 ,0x46 ,0x49 ,0x4c ,0x2f ,0x30,
                        0x34 ,0x38 ,0x88 ,0x47 ,0x4a ,0x4d ,0x31 ,0x32 ,0x00 ,0x87 ,0xe6 ,0x48 ,0x4b ,0x4e ,0x28 ,0x52,
                        0x50 ,0xe5 ,0xe7 ,0xd2 ,0x53 ,0x5f ,0x5c ,0x59 ,0x51 ,0x00 ,0xf1 ,0xd1 ,0x54 ,0x60 ,0x5d ,0x5a,
                        0x4f ,0x8e ,0x65 ,0xd0 ,0x55 ,0x61 ,0x5e ,0x5b ,0x62 ,0xa4 ,0xe4 ,0xfc ,0x56 ,0x57 ,0x85 ,0x58,
                        0x63 ,0x00 ,0x00 ,0xc2 ,0x24};
                    length = 133;
                    break;
                case 0x09:
                    buffer = new byte[] { 0x09 ,0x2b ,0x00 ,0x49 ,0x00 ,0x00 ,0x4a ,0x00 ,0x00 ,0x4b ,0x00 ,0x00 ,0x4c ,0x00 ,0x00 ,0x4d,
                        0x00 ,0x00 ,0x4e ,0x00 ,0x00 ,0xa4 ,0x00 ,0x00 ,0x8e ,0x00 ,0x00 ,0xd0 ,0x00 ,0x00 ,0xd1 ,0x00,
                        0x00 ,0x00 ,0x00 ,0x00 ,0x01 ,0x00 ,0x00 ,0x00 ,0x00 ,0xcd ,0x04};
                    length = 43;
                    break;
                case 0x0d:
                    length = 443;
                    buffer = new byte[] { 0x0d ,0xbb ,0x01 ,0x00 ,0x06 ,0x0b ,0x05 ,0x45 ,0x83 ,0xca ,0xca ,0xca ,0xca ,0xca ,0xca ,0xce,
                            0xce ,0xd2 ,0xce ,0xce ,0xd2 ,0x19 ,0x19 ,0x19 ,0x19 ,0x19 ,0x19 ,0x23 ,0x23 ,0x2d ,0x23 ,0x23,
                            0x2d ,0xe0 ,0xe0 ,0xe0 ,0xe0 ,0xe0 ,0xe0 ,0xe3 ,0xe3 ,0xe6 ,0xe3 ,0xe3 ,0xe6 ,0xd2 ,0xd2 ,0xd5,
                            0xd2 ,0xd2 ,0xd5 ,0xd5 ,0xd5 ,0xd9 ,0xd5 ,0x00 ,0xd9 ,0x2d ,0x2d ,0x36 ,0x2d ,0x2d ,0x36 ,0x36,
                            0x36 ,0x40 ,0x36 ,0x00 ,0x40 ,0xe6 ,0xe6 ,0xe9 ,0xe6 ,0xe6 ,0xe9 ,0xe9 ,0xe9 ,0xec ,0xe9 ,0x00,
                            0xec ,0xd9 ,0xd9 ,0xdd ,0xd9 ,0xdd ,0xdd ,0xe0 ,0xe0 ,0xdd ,0xe0 ,0xe4 ,0xe4 ,0x40 ,0x40 ,0x4a,
                            0x40 ,0x4a ,0x4a ,0x53 ,0x53 ,0x4a ,0x53 ,0x5d ,0x5d ,0xec ,0xec ,0xef ,0xec ,0xef ,0xef ,0xf2,
                            0xf2 ,0xef ,0xf2 ,0xf5 ,0xf5 ,0xe4 ,0xe4 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00,
                            0x00 ,0x5d ,0x5d ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0xf5 ,0xf5 ,0x00,
                            0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0xe4 ,0xe4 ,0xe8 ,0xe8 ,0xe8 ,0xe8 ,0xe8,
                            0xeb ,0xeb ,0xeb ,0x00 ,0xeb ,0x5d ,0x5d ,0x67 ,0x67 ,0x67 ,0x67 ,0x67 ,0x70 ,0x70 ,0x70 ,0x00,
                            0x70 ,0xf5 ,0xf5 ,0xf8 ,0xf8 ,0xf8 ,0xf8 ,0xf8 ,0xfb ,0xfb ,0xfb ,0x00 ,0xfb ,0xeb ,0xef ,0xef,
                            0xef ,0x00 ,0xef ,0xf0 ,0xf0 ,0xed ,0xf0 ,0xf0 ,0x00 ,0x70 ,0x7a ,0x7a ,0x7a ,0x00 ,0x7a ,0x7a,
                            0x7a ,0x6f ,0x7a ,0x7a ,0x00 ,0xfb ,0xfd ,0xfd ,0xfd ,0x00 ,0xfd ,0xf8 ,0xf8 ,0xea ,0xf8 ,0xf8,
                            0x00 ,0xed ,0xed ,0xea ,0xed ,0xed ,0x00 ,0xed ,0xea ,0xea ,0xf6 ,0xe7 ,0xea ,0x6f ,0x6f ,0x65,
                            0x6f ,0x6f ,0x00 ,0x6f ,0x65 ,0x65 ,0x66 ,0x5a ,0x65 ,0xea ,0xea ,0xdc ,0xea ,0xea ,0x00 ,0xea,
                            0xdc ,0xdc ,0x00 ,0xce ,0xdc ,0xea ,0xe7 ,0xe5 ,0xe7 ,0xe5 ,0xe5 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00,
                            0x00 ,0x65 ,0x5a ,0x50 ,0x5a ,0x50 ,0x50 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0xdc ,0xce ,0xc0,
                            0xce ,0xc0 ,0xc0 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0xe7 ,0x00 ,0x00 ,0xe2 ,0xe2 ,0xe2 ,0xe2,
                            0xdf ,0xdf ,0xdf ,0xdf ,0xdf ,0x5a ,0x00 ,0x00 ,0x45 ,0x45 ,0x45 ,0x45 ,0x3b ,0x3b ,0x3b ,0x3b,
                            0x3b ,0xce ,0x00 ,0x00 ,0xb2 ,0xb2 ,0xb2 ,0xb2 ,0xa4 ,0xa4 ,0xa4 ,0xa4 ,0xa4 ,0xdc ,0xdc ,0xdc,
                            0xdc ,0x00 ,0xda ,0xda ,0xda ,0xda ,0xda ,0x00 ,0xd7 ,0x30 ,0x30 ,0x30 ,0x30 ,0x00 ,0x26 ,0x26,
                            0x26 ,0x26 ,0x26 ,0x00 ,0x1c ,0x96 ,0x96 ,0x96 ,0x96 ,0x00 ,0x88 ,0x88 ,0x88 ,0x88 ,0x88 ,0x00,
                            0x7a ,0xd7 ,0xd7 ,0xd7 ,0x00 ,0xd4 ,0xd4 ,0xd4 ,0xd4 ,0xd4 ,0xd1 ,0xd1 ,0xd1 ,0x1c ,0x1c ,0x1c,
                            0x00 ,0x11 ,0x11 ,0x11 ,0x11 ,0x11 ,0x06 ,0x06 ,0x06 ,0x7a ,0x7a ,0x7a ,0x00 ,0x6c ,0x6c ,0x6c,
                            0x6c ,0x6c ,0x5e ,0x5e ,0x5e ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00,
                            0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00,
                            0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x24 ,0xcf};

                    break;
                case 0x13:
                    buffer = new byte[] { 0x13, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    length = 8;
                    break;
            }


            if (buffer == null)
            {
                return false;
            }


            bool success = ctrl_device.WriteFeatureData(buffer);

            return success;
        }
    }
}
