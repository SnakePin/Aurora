using HidSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Aurora.Devices.Drevo
{
    class DrevoKeyboard
    {
        private HidDevice keyboardDevice;
        private HidStream packetStream;
        private bool isConnected = false;

        public struct DrevoProductInfo
        {
            public int VendorID;
            public int ProductID;
            public int LayoutNumber;
        }

        public static readonly IList<DrevoProductInfo> DrevoKeyboardProducts = new ReadOnlyCollection<DrevoProductInfo>
        (new[] {
             new DrevoProductInfo{ VendorID = 0x1A2C, ProductID = 0xB51F, LayoutNumber = 0 },
             new DrevoProductInfo{ VendorID = 0x1A2C, ProductID = 0xB57E, LayoutNumber = 0 },
             new DrevoProductInfo{ VendorID = 0x1A2C, ProductID = 0xB58E, LayoutNumber = 1 },
             new DrevoProductInfo{ VendorID = 0x1A2C, ProductID = 0xB58F, LayoutNumber = 1 },
             new DrevoProductInfo{ VendorID = 0x1A2C, ProductID = 0xB59E, LayoutNumber = 2 },
             new DrevoProductInfo{ VendorID = 0x1A2C, ProductID = 0xB59F, LayoutNumber = 2 },
             new DrevoProductInfo{ VendorID = 0x1A2C, ProductID = 0xB5BE, LayoutNumber = 3 },
             new DrevoProductInfo{ VendorID = 0x1A2C, ProductID = 0xB5BF, LayoutNumber = 3 },
        });

        DrevoKeyboard(HidDevice device)
        {
            keyboardDevice = device;
            LayoutNumber = DrevoKeyboardProducts.First(x => x.VendorID == device.VendorID && x.ProductID == device.ProductID).LayoutNumber;
        }

        ~DrevoKeyboard()
        {
            Disconnect();
        }

        public static IEnumerable<DrevoKeyboard> GetDrevoKeyboards()
        {
            var VIDs = DrevoKeyboardProducts.Select(x => x.VendorID).Distinct();
            var PIDs = DrevoKeyboardProducts.Select(x => x.ProductID).Distinct();

            var devices = DeviceList.Local.GetHidDevices().Where(x =>
            {
                //TODO: x.GetReportDescriptor().DeviceItems.Any(x => x.CollectionType == 4)
                return VIDs.Contains(x.VendorID) && PIDs.Contains(x.ProductID) && x.DevicePath.Contains("col04") && x.GetMaxFeatureReportLength() >= 8;
            });

            return devices.Select(x => new DrevoKeyboard(x));
        }

        public int LayoutNumber { get; private set; }

        public bool Connect()
        {
            if (isConnected)
            {
                return false;
            }

            if (!keyboardDevice.TryOpen(out packetStream))
            {
                return false;
            }
            SendInitPacket();
            isConnected = true;
            return true;
        }

        public bool Disconnect()
        {
            if (!isConnected)
            {
                return false;
            }

            SendDeInitPacket();
            packetStream.Dispose();
            packetStream = null;
            isConnected = false;
            return true;
        }

        public bool SendBitmapToDevice(DrevoBitmap bitmap)
        {
            if (!isConnected)
            {
                return false;
            }

            return bitmap.WriteBitmapToHidStream(packetStream);
        }

        void SendInitPacket()
        {
            WriteToKeyboard(new byte[] { 0x05, 0xFE, 0x0C, 0x0F, 0x00, 0x00, 0x00, 0x00 });
        }

        void SendDeInitPacket()
        {
            WriteToKeyboard(new byte[] { 0x05, 0xFE, 0x01, 0x0F, 0x00, 0x00, 0x00, 0x00 });
        }

        void WriteToKeyboard(byte[] buf)
        {
            packetStream.SetFeature(buf);
        }
    }
}
