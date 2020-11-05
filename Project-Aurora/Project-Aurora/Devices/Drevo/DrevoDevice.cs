using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Aurora.Settings;

namespace Aurora.Devices.Drevo
{
    public class DrevoDevice : DefaultDevice
    {
        public override string DeviceName => "Drevo Radi FOSS";

        private DrevoKeyboard keyboard;

        public override bool Initialize()
        {
            try
            {
                keyboard = DrevoKeyboard.GetDrevoKeyboards().FirstOrDefault();
                if (keyboard == null)
                {
                    throw new Exception("Keyboard not found.");
                }

                return IsInitialized = keyboard.Connect();
            }
            catch (Exception exc)
            {
                LogError($"There was an error initializing Drevo Radi FOSS: {exc.Message}");
                return IsInitialized = false;
            }
        }

        public override void Shutdown()
        {
            keyboard.Disconnect();
            IsInitialized = false;
        }

        private DrevoBitmap bitmap_snk = new DrevoBitmap();

        public override bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, DoWorkEventArgs e, bool forced = false)
        {
            foreach (var key in keyColors)
            {
                /*
                 * 87-100 top left to right
                 * 102-106 right top to bottom
                 * 108-121 bottom right to left
                 * 122-126 left bottom to top
                */
                var keyMappingsCustom = new List<KeyValuePair<DeviceKeys, int>>();
                void MapLightbarTo(DeviceKeys deviceKey, int i1, int i2) { for (int i = i1; i <= i2; i++) { keyMappingsCustom.Add(new KeyValuePair<DeviceKeys, int>(deviceKey, i)); } }
                void MapLightbarToMULTI(DeviceKeys d1, int i1, int i2) { for (int i = i1; i <= i2; i++) { keyMappingsCustom.Add(new KeyValuePair<DeviceKeys, int>((DeviceKeys)((int)d1 + (i - i1)), i)); } }
                if (keyboard.LayoutNumber == 0) //Blademaster TE(?)
                {
                    //BOTTOM
                    MapLightbarTo(DeviceKeys.LEFT_CONTROL, 121, 121);
                    MapLightbarTo(DeviceKeys.LEFT_WINDOWS, 120, 120);
                    MapLightbarTo(DeviceKeys.LEFT_ALT, 119, 119);
                    MapLightbarTo(DeviceKeys.SPACE, 115, 118);
                    MapLightbarTo(DeviceKeys.RIGHT_ALT, 114, 114);
                    MapLightbarTo(DeviceKeys.APPLICATION_SELECT, 113, 113);
                    MapLightbarTo(DeviceKeys.FN_Key, 112, 112);
                    MapLightbarTo(DeviceKeys.RIGHT_CONTROL, 111, 111);
                    MapLightbarTo(DeviceKeys.ARROW_LEFT, 110, 110);
                    MapLightbarTo(DeviceKeys.ARROW_DOWN, 109, 109);
                    MapLightbarTo(DeviceKeys.ARROW_RIGHT, 108, 108);

                    //TOP
                    MapLightbarToMULTI(DeviceKeys.ESC, 87, 100);

                    //LEFT
                    MapLightbarTo(DeviceKeys.LEFT_CONTROL, 122, 122);
                    MapLightbarTo(DeviceKeys.LEFT_SHIFT, 123, 123);
                    MapLightbarTo(DeviceKeys.CAPS_LOCK, 124, 124);
                    MapLightbarTo(DeviceKeys.TAB, 125, 125);
                    MapLightbarTo(DeviceKeys.TILDE, 126, 126);

                    //RIGHT
                    MapLightbarTo(DeviceKeys.PAUSE_BREAK, 102, 102);
                    MapLightbarTo(DeviceKeys.PAGE_UP, 103, 103);
                    MapLightbarTo(DeviceKeys.PAGE_DOWN, 104, 104);
                    MapLightbarTo(DeviceKeys.ARROW_RIGHT, 105, 105);
                    MapLightbarTo(DeviceKeys.ARROW_RIGHT, 106, 106);
                }

                var customMapping = keyMappingsCustom.Where(x => x.Key == key.Key);
                foreach (var item in customMapping)
                {
                    bitmap_snk.SetColorByDrevoIndex(item.Value, key.Value.R, key.Value.G, key.Value.B);
                }

                int index = AuroraToDrevoBitmap((int)key.Key, keyboard.LayoutNumber);
                if (index != -1)
                {
                    bitmap_snk.SetColorByDrevoIndex(index, key.Value.R, key.Value.G, key.Value.B);
                }
            }

            return keyboard.SendBitmapToDevice(bitmap_snk);
        }

        private static int AuroraToDrevoBitmap(int index, int layoutNum)
        {
            if (layoutNum == 0)
            {
                switch (index)
                {
                    case 1: return 0;
                    case 2: return 1;
                    case 3: return 2;
                    case 4: return 3;
                    case 5: return 4;
                    case 6: return 5;
                    case 7: return 6;
                    case 8: return 7;
                    case 9: return 8;
                    case 10: return 9;
                    case 11: return 10;
                    case 12: return 11;
                    case 13: return 12;
                    case 14: return 13;
                    case 15: return 14;
                    case 16: return 15;
                    case 17: return 16;
                    case 18: return 17;
                    case 19: return 18;
                    case 20: return 19;
                    case 21: return 20;
                    case 22: return 21;
                    case 23: return 22;
                    case 24: return 23;
                    case 25: return 24;
                    case 26: return 25;
                    case 27: return 26;
                    case 28: return 27;
                    case 29: return 28;
                    case 30: return 29;
                    case 31: return 30;
                    case 32: return 31;
                    case 33: return 32;
                    case 38: return 33;
                    case 39: return 34;
                    case 40: return 35;
                    case 41: return 36;
                    case 42: return 37;
                    case 43: return 38;
                    case 44: return 39;
                    case 45: return 40;
                    case 46: return 41;
                    case 47: return 42;
                    case 48: return 43;
                    case 49: return 44;
                    case 50: return 45;
                    case 51: return 46;
                    case 52: return 47;
                    case 53: return 48;
                    case 54: return 49;
                    case 59: return 50;
                    case 60: return 51;
                    case 61: return 52;
                    case 62: return 53;
                    case 63: return 54;
                    case 64: return 55;
                    case 65: return 56;
                    case 66: return 57;
                    case 67: return 58;
                    case 68: return 59;
                    case 69: return 60;
                    case 70: return 61;
                    case 72: return 62;
                    case 76: return 63;
                    case 78: return 64;
                    case 79: return 65;
                    case 80: return 66;
                    case 81: return 67;
                    case 82: return 68;
                    case 83: return 69;
                    case 84: return 70;
                    case 85: return 71;
                    case 86: return 72;
                    case 87: return 73;
                    case 88: return 74;
                    case 89: return 75;
                    case 94: return 76;
                    case 95: return 77;
                    case 96: return 78;
                    case 97: return 79;
                    case 98: return 80;
                    case 100: return 81;
                    case 101: return 83;
                    case 102: return 84;
                    case 103: return 85;
                    case 104: return 86;
                    case 107: return 82;
                }
            }
            else if (layoutNum == 1)
            {
                switch (index)
                {
                    case 1: return 0;
                    case 2: return 1;
                    case 3: return 2;
                    case 4: return 3;
                    case 5: return 4;
                    case 6: return 5;
                    case 7: return 6;
                    case 8: return 7;
                    case 9: return 8;
                    case 10: return 9;
                    case 11: return 10;
                    case 12: return 11;
                    case 13: return 12;
                    case 14: return 13;
                    case 15: return 14;
                    case 16: return 15;
                    case 17: return 16;
                    case 18: return 17;
                    case 19: return 18;
                    case 20: return 19;
                    case 21: return 20;
                    case 22: return 21;
                    case 23: return 22;
                    case 24: return 23;
                    case 25: return 24;
                    case 26: return 25;
                    case 27: return 26;
                    case 28: return 27;
                    case 29: return 28;
                    case 30: return 29;
                    case 31: return 30;
                    case 32: return 31;
                    case 33: return 32;
                    case 38: return 33;
                    case 39: return 34;
                    case 40: return 35;
                    case 41: return 36;
                    case 42: return 37;
                    case 43: return 38;
                    case 44: return 39;
                    case 45: return 40;
                    case 46: return 41;
                    case 47: return 42;
                    case 48: return 43;
                    case 49: return 44;
                    case 50: return 45;
                    case 52: return 47;
                    case 53: return 48;
                    case 54: return 49;
                    case 59: return 50;
                    case 60: return 51;
                    case 61: return 52;
                    case 62: return 53;
                    case 63: return 54;
                    case 64: return 55;
                    case 65: return 56;
                    case 66: return 57;
                    case 67: return 58;
                    case 68: return 59;
                    case 69: return 60;
                    case 70: return 61;
                    case 71: return 62;
                    case 72: return 46;
                    case 76: return 63;
                    case 77: return 64;
                    case 78: return 65;
                    case 79: return 66;
                    case 80: return 67;
                    case 81: return 68;
                    case 82: return 69;
                    case 83: return 70;
                    case 84: return 71;
                    case 85: return 72;
                    case 86: return 73;
                    case 87: return 74;
                    case 88: return 75;
                    case 89: return 76;
                    case 94: return 77;
                    case 95: return 78;
                    case 96: return 79;
                    case 97: return 80;
                    case 98: return 81;
                    case 100: return 82;
                    case 101: return 84;
                    case 102: return 85;
                    case 103: return 86;
                    case 104: return 87;
                    case 107: return 83;
                }
            }
            else if (layoutNum == 3)
            {
                switch (index)
                {
                    case 1: return 0;
                    case 2: return 1;
                    case 3: return 2;
                    case 4: return 3;
                    case 5: return 4;
                    case 6: return 5;
                    case 7: return 6;
                    case 8: return 7;
                    case 9: return 8;
                    case 10: return 9;
                    case 11: return 10;
                    case 12: return 11;
                    case 13: return 12;
                    case 14: return 13;
                    case 15: return 14;
                    case 16: return 15;
                    case 17: return 16;
                    case 18: return 17;
                    case 19: return 18;
                    case 20: return 19;
                    case 21: return 20;
                    case 22: return 21;
                    case 23: return 22;
                    case 24: return 23;
                    case 25: return 24;
                    case 26: return 25;
                    case 27: return 26;
                    case 28: return 27;
                    case 29: return 28;
                    case 30: return 30;
                    case 31: return 31;
                    case 32: return 32;
                    case 33: return 33;
                    case 38: return 34;
                    case 39: return 35;
                    case 40: return 36;
                    case 41: return 37;
                    case 42: return 38;
                    case 43: return 39;
                    case 44: return 40;
                    case 45: return 41;
                    case 46: return 42;
                    case 47: return 43;
                    case 48: return 44;
                    case 49: return 45;
                    case 50: return 46;
                    case 52: return 48;
                    case 53: return 49;
                    case 54: return 50;
                    case 59: return 51;
                    case 60: return 52;
                    case 61: return 53;
                    case 62: return 54;
                    case 63: return 55;
                    case 64: return 56;
                    case 65: return 57;
                    case 66: return 58;
                    case 67: return 59;
                    case 68: return 60;
                    case 69: return 61;
                    case 70: return 62;
                    case 71: return 63;
                    case 72: return 47;
                    case 76: return 64;
                    case 77: return 75;
                    case 78: return 65;
                    case 79: return 66;
                    case 80: return 67;
                    case 81: return 68;
                    case 82: return 69;
                    case 83: return 70;
                    case 84: return 71;
                    case 85: return 72;
                    case 86: return 73;
                    case 87: return 74;
                    case 88: return 76;
                    case 89: return 77;
                    case 94: return 78;
                    case 95: return 79;
                    case 96: return 80;
                    case 97: return 82;
                    case 98: return 83;
                    case 100: return 85;
                    case 101: return 87;
                    case 102: return 88;
                    case 103: return 89;
                    case 104: return 90;
                    case 107: return 86;
                    case 153: return 81;
                    case 155: return 84;
                    case 156: return 29;
                }
            }

            return -1;
        }
    }
}
