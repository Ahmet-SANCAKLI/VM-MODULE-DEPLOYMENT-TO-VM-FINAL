using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sensor_Reading
{
    class ANPacket
    {
        public const int PACKET_ID_ACKNOWLEDGE = 0;
        public const int PACKET_ID_REQUEST = 1;
        public const int PACKET_ID_BOOT_MODE = 2;
        public const int PACKET_ID_DEVICE_INFORMATION = 3;
        public const int PACKET_ID_RESTORE_FACTORY_SETTINGS = 4;
        public const int PACKET_ID_RESET = 5;
        public const int PACKET_ID_PRINT = 6;
        public const int PACKET_ID_FILE_TRANSFER_REQUEST = 7;
        public const int PACKET_ID_FILE_TRANSFER_ACKNOWLEDGE = 8;
        public const int PACKET_ID_FILE_TRANSFER = 9;

        public const int PACKET_ID_SYSTEM_STATE = 20;
        public const int PACKET_ID_RAW_SENSORS = 28;
        public const int PACKET_ID_HEAVE = 58;

        public int id;
        public int length;
        public byte[] data;

        public ANPacket(int length, int id)
        {
            this.length = length;
            this.id = id;
            this.data = new byte[length];
        }
    }
}
