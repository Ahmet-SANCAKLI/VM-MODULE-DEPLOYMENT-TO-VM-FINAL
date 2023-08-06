# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for
# full license information.

# Modbus/TCP server with start/stop schedule
import argparse
import asyncio
import time
import signal
import threading
from email.message import Message
import sys
import json
import os
from pyModbusTCP.server import ModbusServer, DataBank
from azure.iot.device.aio import IoTHubModuleClient

# Event indicating client stop
stop_event = threading.Event()

# Global variables
msg = Message()
x = 0
y = 0

def create_client():
    client = IoTHubModuleClient.create_from_edge_environment()

    # Define function for handling received messages
    async def receive_message_handler(message):
        global x
        x = x + 1

        print(f"message #{x}")

        if message.input_name == "input1":
            await client.send_message_to_output(message, "output1")
            global msg
            msg = message
            # Extract sensor data from the incoming JSON message
            data = json.loads(msg.data)

            # Extract individual sensor data
            roll = data['roll']
            roll_sign = "1" if roll < 0 else "0"

            pitch = data['pitch']
            pitch_sign = "1" if pitch < 0 else "0"

            heading = data['heading']
            heading_sign = "1" if heading < 0 else "0"

            gForcemagnitude = data['gForcemagnitude']
            gForcemagnitude_sign = "1" if gForcemagnitude < 0 else "0"

            east_offset = data['east_offset']
            east_offset_sign = "1" if east_offset < 0 else "0"

            north_offset = data['north_offset']
            north_offset_sign = "1" if north_offset < 0 else "0"

            total_offset = data['total_offset']
            total_offset_sign = "1" if total_offset < 0 else "0"

            # Convert sensor data to Modbus format and update DataBank
            DataBank.set_words(0, [int(roll * 1000)])   # *1000
            DataBank.set_words(1, [int(pitch * 1000)])  # *1000
            DataBank.set_words(2, [int(heading * 100)])
            DataBank.set_words(3, [int(gForcemagnitude * 100)])
            DataBank.set_words(4, [int(east_offset * 100)])
            DataBank.set_words(5, [int(north_offset * 100)])
            DataBank.set_words(6, [int(total_offset * 100)])

            # Concatenate sign values to a single string and convert it to binary
            sign_string = roll_sign + pitch_sign + heading_sign + gForcemagnitude_sign + east_offset_sign + north_offset_sign + total_offset_sign
            sign_binary = int(sign_string, 2)

            # Set the sign binary value in the DataBank
            DataBank.set_words(7, [sign_binary])

    try:
        # Set handler on the client
        client.on_message_received = receive_message_handler
    except:
        # Cleanup if failure occurs
        client.shutdown()
        raise

    return client

def alive_word_job():
    global msg

    # Truncate list of existing files
    list_of_files = os.listdir('/home/moduleuser/')
    while len(list_of_files) >= 4:
        full_paths = [f"/home/moduleuser/{x}" for x in list_of_files]
        oldest_file = min(full_paths, key=os.path.getctime)
        os.remove(os.path.abspath(oldest_file))

    # Check if there is exceedance of 25 files
    if len(list_of_files) > 25:
        try:
            timestr = time.strftime("%Y%m%d-%H%M%S")
            with open(f"/home/moduleuser/{timestr}", 'w') as f:
                f.write('a new text file is created!')

        except OSError as e:
            print("Error occurred during file writing:", e)


async def run_sample(client):
    # Parse args
    parser = argparse.ArgumentParser()
    parser.add_argument("-H", "--host", type=str, default="0.0.0.0", help="Host")
    parser.add_argument("-p", "--port", type=int, default=1502, help="TCP port")
    args = parser.parse_args()

    # Init Modbus server and start it
    server = ModbusServer(host=args.host, port=args.port, no_block=True)
    server.start()

    global y
    while True:
        alive_word_job()
        y += 1
        print(f"main loop #{y}")

        # Do your other operations here without awaiting
        # ...

        # You can add a delay here if needed, but it's not required
        # await asyncio.sleep(5)

def main():
    if not sys.version >= "3.5.3":
        raise Exception("The sample requires Python 3.5.3+. Current version of Python:", sys.version)

    print("IoT Hub Client for Python")

    # NOTE: Client is implicitly connected due to the handler being set on it
    client = create_client()

    # Define a handler to cleanup when module is terminated by Edge
    def module_termination_handler(signal, frame):
        print("IoTHubClient sample stopped by Edge")
        stop_event.set()

    # Set the Edge termination handler
    signal.signal(signal.SIGTERM, module_termination_handler)

    # Run the sample
    loop = asyncio.get_event_loop()
    try:
        loop.run_until_complete(run_sample(client))
    except Exception as e:
        print("Unexpected error:", e)
        raise
    finally:
        print("Shutting down IoT Hub Client...")
        loop.run_until_complete(client.shutdown())
        loop.close()

if __name__ == "__main__":
    main()
