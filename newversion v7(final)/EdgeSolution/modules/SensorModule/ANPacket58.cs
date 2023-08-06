using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sensor_Reading
{
    class ANPacket58
    {

        public float[] heave;

        public ANPacket58()
        {
            heave = new float[4];
            heave[0] = 0;
            heave[1] = 0;
            heave[2] = 0;
            heave[3] = 0;

        }

        public ANPacket58(ANPacket packet)
        {
            heave = new float[4];
            heave[0] = BitConverter.ToSingle(packet.data, 0);
            heave[1] = BitConverter.ToSingle(packet.data, 4);
            heave[2] = BitConverter.ToSingle(packet.data, 8);
            heave[3] = BitConverter.ToSingle(packet.data, 12);

        }
    }
}
