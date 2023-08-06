namespace SampleModule
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Ports;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Sensor_Reading;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Newtonsoft.Json;
    using Microsoft.Azure.Devices.Shared;
    using System.Net.Sockets;
    //using Microsoft.Azure.Storage.Queues;
    
    

    class Program
    {
        private const int RATE = 1000;

        private const double latitude1=29.10798914; //Marlin nominal latitude in degree

        private const double longitude1=-87.94334921; //Marlin nominal longitude in degree

        private const double rollcorrection=0.23; //roll initial value correction
        private const double pitchcorrection=0.65; //pitch initial value correction
        private const double headingcorrection=-3.0; //heading initial value correction
        
        private static ANPacketDecoder anPacketDecoder = new ANPacketDecoder();
        private static bool keepRunning = true;

        private static int counter;

        private static ModuleClient ioTHubModuleClient;

        private static Message commandMessage1;
        private static Message commandMessage2;
        private static Message commandMessage3;
        private static string data1;
        private static string data2;
        private static string data3;

        private static DeviceClient deviceClient;

        private static DateTime lastSentTime;

        private static List<PositionMessage> unsentPositions = new List<PositionMessage>();

        private static Socket client = null;
        private static Boolean networkConnected = false;
        

        static void sendStringMsg(PositionMessage msgText) {
            //string text = DateTime.Now.ToString() + "," + msgText;           
            using(Message msg = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msgText)))) {
                //ioTHubModuleClient.SendEventAsync("output1", msg).ConfigureAwait(false);
                deviceClient.SendEventAsync(msg);
                
            }
        }


        private static void networkReceiveCallback(IAsyncResult ar)
        {
            try
            {
                anPacketDecoder.bufferLength += client.EndReceive(ar);
                decodePackets();
                client.BeginReceive(anPacketDecoder.buffer, anPacketDecoder.bufferLength, anPacketDecoder.buffer.Length - anPacketDecoder.bufferLength, 0, new AsyncCallback(networkReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        static void Main(string[] args)
        {
            
            Console.WriteLine("Connecting!");
            
            Init().Wait();

            //string portName="ttyUSB0";
            //int baudrate = 115200;
            
            deviceClient = DeviceClient.CreateFromConnectionString("HostName=HM-HUB.azure-devices.net;DeviceId=HMedge;SharedAccessKey=Zu9IYVqYKUhvxYLZWTnoJlyzCwn6Mqkd/vGrOm9fnX4=");
            data1 = "Opened the serial port succesfully!!!!!!!!";  
            data2 = "An error occured while opening the serial port!!!!!"; 
            data3 = "Running!!"; 
            var serializeData1 = JsonConvert.SerializeObject(data1);  
            var serializeData2 = JsonConvert.SerializeObject(data2); 
            var serializeData3 = JsonConvert.SerializeObject(data3); 
            commandMessage1 = new Message(Encoding.ASCII.GetBytes(serializeData1));  
            commandMessage2 = new Message(Encoding.ASCII.GetBytes(serializeData2));
            commandMessage3 = new Message(Encoding.ASCII.GetBytes(serializeData3));         
            //Console.WriteLine("Send Message: {0}", serializeData);
            // try to setup an async callback for the packet data
            // start running
            if (!networkConnected)
            {
                try
                {
                    // Create a TCP/IP socket.  
                    client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Connect to the remote endpoint.  
                    client.Connect("192.168.1.129", 16718);
                    client.BeginReceive(anPacketDecoder.buffer, anPacketDecoder.bufferLength, anPacketDecoder.buffer.Length - anPacketDecoder.bufferLength, 0, new AsyncCallback(networkReceiveCallback), null);

                    networkConnected = true;
                    Console.WriteLine("Connected!!!");
                    //buttonSerialConnect.Enabled = false;
                    //buttonNetworkConnect.Text = "Disconnect";
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.ToString());
                }
            }
            else
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                networkConnected = false;
            }
            
            while(keepRunning)
            {
                
                //serialPort1_DataReceived();
                Thread.Sleep(100);
            }

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
            
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            lastSentTime = DateTime.Now;
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            // Register callback to be called when a message is received by the module
            //await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", PipeMessage, ioTHubModuleClient);
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref counter);

            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

            if (!string.IsNullOrEmpty(messageString))
            {
                using (var pipeMessage = new Message(messageBytes))
                {
                    foreach (var prop in message.Properties)
                    {
                        pipeMessage.Properties.Add(prop.Key, prop.Value);
                    }
                    await moduleClient.SendEventAsync("output1", pipeMessage);
                
                    Console.WriteLine("Received message sent");
                }
            }
            return MessageResponse.Completed;
        }
    



        static async Task Send_Masseage_toHub(string text)
        {
          Message msg = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(text)));
          await  ioTHubModuleClient.SendEventAsync("output1", msg);
        }

        public static double GetDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
        {
            var d1 = latitude * (Math.PI / 180.0);
            var num1 = longitude * (Math.PI / 180.0);
            var d2 = otherLatitude * (Math.PI / 180.0);
            var num2 = otherLongitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
            
            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3))) * 3.28084; // Distance in feet
        }
        private static void decodePackets()
        {
            // log decode message called
                //sendStringMsg("decodePackets(): called");

            int packets = 0;
            
            ANPacket packet = null;
            while ((packet = anPacketDecoder.packetDecode()) != null)
            {
                // log processing packet number
                //sendStringMsg("     processing packet number: " + packets);
                switch (packet.id)
                {
                    case ANPacket.PACKET_ID_SYSTEM_STATE:
                        if (packet.length == 100)
                        {
                            
                            ANPacket20 anPacket20 = new ANPacket20(packet);
                            
                            double latitude = (anPacket20.position[0] * 180 / Math.PI); // degree
                            double longitude = (anPacket20.position[1] * 180 / Math.PI); //  degree
                            double height = (anPacket20.position[2]);                    //  meters
                            double roll = (anPacket20.orientation[0] * 180 / Math.PI)+rollcorrection;   // degree
                            double pitch = (anPacket20.orientation[1] * 180 / Math.PI)+pitchcorrection;  // degree 
                            double heading = (anPacket20.orientation[2] * 180 / Math.PI)+headingcorrection; // degree 
                            double gForcemagnitude = anPacket20.gForce;
                            DateTime time = DateTime.Now;

                            //string text = latitude.ToString()+","+longitude.ToString()+","+height.ToString()+","+roll.ToString()+","+pitch.ToString()+","+heading.ToString()+","+time.ToString();
                            //Send_Masseage_toHub(text).Wait();
                            double north_offset = -1*(GetDistance(longitude,latitude1,longitude,latitude) - 70.01); // 70.01 ft correction for the antenna location
                            double east_offset = 1*(GetDistance(longitude1,latitude,longitude,latitude) -12.02); // 12.02 ft correction for the antenna location
                            double total_offset = Math.Sqrt(east_offset*east_offset+north_offset*north_offset); // Total offset

                            PositionMessage positionMessage= new PositionMessage(latitude,longitude,height,roll,
                                pitch,heading, gForcemagnitude, east_offset, north_offset, total_offset, time.AddHours(-5)
                            );
                            unsentPositions.Add(positionMessage);

                            DateTime now = DateTime.Now;
                            TimeSpan diff = now-lastSentTime;
                            double milli = Math.Abs(diff.TotalMilliseconds);
                            if(milli>RATE) {        
                                using(Message msg = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(unsentPositions)))) {
                                        
                                        
                                     
                                        msg.ContentType = "application/json";
                                        msg.ContentEncoding = "utf-8";
                                       // msg.Properties.Add()
                                        Console.WriteLine($"time is {milli}");
                                        Console.WriteLine($"roll is {roll}");
                                        //deviceClient.SendEventAsync(msg);
                                        ioTHubModuleClient.SendEventAsync("output1", msg);
                                        lastSentTime = now;
                                        unsentPositions.Clear();
                                }
                            }

                // string messageBody = JsonSerializer.Serialize(msg);
                // using var message = new Message(Encoding.ASCII.GetBytes(messageBody))
                // {
                //     ContentType = "application/json",
                //     ContentEncoding = "utf-8",
                // };


                            //sendStringMsg("     ************************************************** - package handled: " + packets);
                            
                            //deviceClient.SendEventAsync(commandMessage3); 
                            
                             //using(Stream messageStream = positionMessage.toJSONStream().Result) {
                                
                                // using (var posMsg = new Message(messageStream)) {
                                     //ioTHubModuleClient.SendEventAsync("output1", posMsg);
                                      //Console.WriteLine("data point logged");
                                 //}
                            // }
                        } else {
                            // log package size not handled
                            //sendStringMsg("     --------- package size not handled: " + packet.length);
                        }
                        break;

                    // case ANPacket.PACKET_ID_RAW_SENSORS:
                    //     if (packet.length == 48)
                    //     {
                    //         ANPacket28 anPacket28 = new ANPacket28(packet);
                    //         // write to file
                    //     }
                    //     break;

                    // case ANPacket.PACKET_ID_HEAVE:
                    //     if (packet.length == 16)
                    //     {
                    //         ANPacket58 anPacket58 = new ANPacket58(packet);
                    //         // write to file

                    //     }
                    //     break;

                    default: 
                        // log no message created, package id not hadled
                        //sendStringMsg("    ######## package id not handled: " + packet.id);
                        break;
                }

                packets++;
            }

            // this function is exiting and how many messages were sent this call (when), total number of packages processed
            //sendStringMsg("total number of packages processed: " + packets);
        }
    }
}
