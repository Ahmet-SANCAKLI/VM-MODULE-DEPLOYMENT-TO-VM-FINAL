using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sensor_Reading
{
    class ANPacket28
    {
        public float[] accelerometers;
        public float[] gyroscopes;
        public float[] magnetometers;
        public float imuTemperature;
        public float pressure;
        public float pressureTemperature;

        public ANPacket28()
        {
            accelerometers = new float[3];
            accelerometers[0] = 0;
            accelerometers[1] = 0;
            accelerometers[2] = 0;
            gyroscopes = new float[3];
            gyroscopes[0] = 0;
            gyroscopes[1] = 0;
            gyroscopes[2] = 0;
            magnetometers = new float[3];
            magnetometers[0] = 0;
            magnetometers[1] = 0;
            magnetometers[2] = 0;
            imuTemperature = 0;
            pressure = 0;
            pressureTemperature = 0;
        }

        public ANPacket28(ANPacket packet)
        {
            accelerometers = new float[3];
            accelerometers[0] = BitConverter.ToSingle(packet.data, 0);
            accelerometers[1] = BitConverter.ToSingle(packet.data, 4);
            accelerometers[2] = BitConverter.ToSingle(packet.data, 8);
            gyroscopes = new float[3];
            gyroscopes[0] = BitConverter.ToSingle(packet.data, 12);
            gyroscopes[1] = BitConverter.ToSingle(packet.data, 16);
            gyroscopes[2] = BitConverter.ToSingle(packet.data, 20);
            magnetometers = new float[3];
            magnetometers[0] = BitConverter.ToSingle(packet.data, 24);
            magnetometers[1] = BitConverter.ToSingle(packet.data, 28);
            magnetometers[2] = BitConverter.ToSingle(packet.data, 32);
            imuTemperature = BitConverter.ToSingle(packet.data, 36);
            pressure = BitConverter.ToSingle(packet.data, 40);
            pressureTemperature = BitConverter.ToSingle(packet.data, 44);
        }
    }
}
