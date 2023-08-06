namespace SampleModule
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;


    class PositionMessage {
        public double latitude {get; set;}
        public double longitude {get; set;}
        public double height {get; set;}
        public double roll {get; set;}
        public double pitch {get; set;}
        public double heading {get; set;}
        public double gForcemagnitude {get; set;}
        public double east_offset {get; set;}
        public double north_offset {get; set;}
        public double total_offset {get; set;}
        public DateTime timestamp {get; set;}
        

        public PositionMessage(double latitude, double longitude,
            double height, double roll, double pitch, double heading, double gForcemagnitude,
            double east_offset, double north_offset, double total_offset, DateTime timestamp
        )  {
            this.latitude = latitude;
            this.longitude = longitude;
            this.height = height;
            this.roll = roll;
            this.pitch = pitch;
            this.heading = heading;
            this.gForcemagnitude = gForcemagnitude;
            this.east_offset = east_offset;
            this.north_offset = north_offset;
            this.total_offset = total_offset;
            this.timestamp = timestamp;
        }

        public async Task<Stream> toJSONStream() {
            var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync<PositionMessage>(stream, this);
            stream.Position = 0;

            return stream;
        }

        public static PositionMessage fromJSONStream(Stream stream) {
            return null;
        }
    }

}