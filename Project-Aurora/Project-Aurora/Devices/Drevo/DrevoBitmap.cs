using HidSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurora.Devices.Drevo
{

    class DrevoBitmap
    {
        const int bufferSize = 392;
        static readonly byte[] bufferHeader = new byte[] { 0xF3, 0x01, 0x00, 0x7F };

        private byte[] buffer = new byte[bufferSize];

        public DrevoBitmap()
        {
            BitmapReset();
        }

        public void BitmapReset()
        {
            Array.Clear(buffer, 0, buffer.Length);
            bufferHeader.CopyTo(buffer, 0);
        }

        public bool SetColorByDrevoIndex(int drevoIdx, byte R, byte G, byte B)
        {
            int bitmapRgbIndex = drevoIdx * 3 + 4; //Multiply by 3 because each key's color is 3 bytes, plus 4 cause the header is 4 bytes
            if (bitmapRgbIndex >= buffer.Length)
            {
                return false;
            }

            buffer[bitmapRgbIndex] = R;
            buffer[++bitmapRgbIndex] = G;
            buffer[++bitmapRgbIndex] = B;

            return true;
        }

        public bool WriteBitmapToHidStream(HidStream hidStrm)
        {
            if (!hidStrm.CanWrite)
            {
                return false;
            }

            int bitmapArrayIndex = 0;
            int requiredPacketCount = buffer.Length / 6;
            for (int sentPacketCount = 0; sentPacketCount < requiredPacketCount; sentPacketCount++)
            {
                byte[] dataBuffer = new byte[8];

                dataBuffer[0] = 5; // set report ID to 5
                Array.Copy(buffer, bitmapArrayIndex + 4, dataBuffer, 5, 3); //second key's rgb value

                if (sentPacketCount > 0)
                {
                    Array.Copy(buffer, bitmapArrayIndex, dataBuffer, 2, 4); // set 3,4,5,6th bytes to 4 bytes from buffer[bitmapArrayIndex]
                    bitmapArrayIndex += 6;
                }
                else
                {
                    Array.Copy(buffer, bitmapArrayIndex, dataBuffer, 1, 4); // set 2,3,4,5th bytes to 4 bytes from buffer[bitmapArrayIndex]
                    dataBuffer[7] = 0;
                    bitmapArrayIndex += 7;
                }

                hidStrm.SetFeature(dataBuffer);
            }

            return true;
        }

    }
}
