import asyncio
import json
import logging
import threading
import os
from datetime import datetime
from azure.iot.device.aio import IoTHubModuleClient
import pandas as pd

# Event indicating client stop
stop_event = threading.Event()

# Logger'ı yapılandırın
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

x = 0
y = 0

column_details = {
    'roll_mean': [],
    'pitch_mean': []
}
# creating a Dataframe object
static_mean_df = pd.DataFrame(column_details)

# IoT Hub bağlantısı için ModuleClient oluşturun
client = IoTHubModuleClient.create_from_edge_environment()

# Set the message handler
async def receive_message_handler(message):
    global x
    x += 1
    global static_mean_df
    #logger.info("recorded data is: ")
    logger.info(f"message #{x}")

    try:
        # Mesaj verisini JSON'dan Python sözlüğüne dönüştürün
        message_data = json.loads(message.data)
        logger.info(f"Received message: {message_data}")  # Alınan mesajı logla

        if message_data.get("deviceId") == "ModbusClientModule":
            # Veriyi DataFrame'e çevir
            if isinstance(message_data["data"], list):
                df = pd.DataFrame(message_data["data"])
            else:
                df = pd.DataFrame([message_data["data"]])  # Veriyi bir dizi içine alarak DataFrame'e dönüştür

            logger.info("DataFrame: %s", df)  # DataFrame'in içeriğini logla

            # Ortalama hesaplamaları
            roll_mean = df['roll'].mean()
            pitch_mean = df['pitch'].mean()
            heading_mean = df['heading'].mean()
            gForcemagnitude_mean = df['gForcemagnitude'].mean()
            east_offset_mean = df['east_offset'].mean()
            north_offset_mean = df['north_offset'].mean()
            total_offset_mean = df['total_offset'].mean()

            # Hesaplanan ortalama değerleri logla
            logger.info(f"Roll Mean: {roll_mean}")
            logger.info(f"Pitch Mean: {pitch_mean}")
            logger.info(f"Heading Mean: {heading_mean}")
            logger.info(f"gForcemagnitude Mean: {gForcemagnitude_mean}")
            logger.info(f"east_offset Mean: {east_offset_mean}")
            logger.info(f"north_offset Mean: {north_offset_mean}")
            logger.info(f"total_offset Mean: {total_offset_mean}")

            # Tarih, saat, dakika ve saniyeyi al
            timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')

            # Mesajı oluştur ve ekrana bas
            message_data = {
                'timestamp': timestamp,
                'roll_mean': roll_mean,
                'pitch_mean': pitch_mean,
                'heading_mean': heading_mean,
                'gForcemagnitude_mean': gForcemagnitude_mean,
                'east_offset_mean': east_offset_mean,
                'north_offset_mean': north_offset_mean,
                'total_offset_mean': total_offset_mean
            }
            logger.info("Message to be sent: %s", message_data)

            # Mesajı IoT Hub'a gönder
            response = await client.send_message_to_output(json.dumps(message_data), "output1")
            logger.info("Message sent to IoT Hub. response: %s", response)

            # Static_mean_df'ye verileri ekleyin
            df_instance = {
                'roll_mean': roll_mean,
                'pitch_mean': pitch_mean
            }
            static_mean_df = static_mean_df.append(df_instance, ignore_index=True)

            if len(static_mean_df.index) > 500:
                static_mean_df = static_mean_df.iloc[1:, :]

            static_mean_roll = static_mean_df['roll_mean'].mean()
            static_mean_pitch = static_mean_df['pitch_mean'].mean()

            logger.info(f"static_mean_roll, static_mean_pitch: {static_mean_roll}, {static_mean_pitch}")
            logger.info(static_mean_df)

            # Static_mean_df'yi CSV dosyasına kaydedin
            save_static_mean_df_to_csv()

            # Yeni klasörü oluşturun ve log kaydı oluşturun
            folder = "folder"
            if not os.path.exists(folder):
                os.makedirs(folder)
                logger.info("New folder '%s' has been created.", folder)

            # Dosyanın tam yolunu belirleyin
            file_path = os.path.join(folder, "static_mean.csv")

            # Veriyi yeni dosyaya kaydedin
            static_mean_df.to_csv(file_path, index=False)

            logger.info("Data with Roll Mean and Pitch Mean has been written to CSV file.")

            # Global değişkenleri güncelleyin
            global y
            y += 1
            msg = message

    except json.JSONDecodeError as json_err:
        logger.error("JSON decoding error: %s", json_err)
    except KeyError as key_err:
        logger.error("Key error: %s", key_err)
    except Exception as ex:
        logger.error("Error while processing the message: %s", ex)

def save_static_mean_df_to_csv():
    try:
        # Static_mean_df'yi CSV dosyasına kaydedin
        folder = "folder"
        if not os.path.exists(folder):
            os.makedirs(folder)
            logger.info("New folder '%s' has been created.", folder)

        # Dosyanın tam yolunu belirleyin
        file_path = os.path.join(folder, "static_mean.csv")

        # Veriyi yeni dosyaya kaydedin
        static_mean_df.to_csv(file_path, index=False)

        logger.info("Data in static_mean_df has been written to CSV file.")

    except Exception as ex:
        logger.error("Error while writing DataFrame to CSV: %s", ex)

async def main():
    try:
        # Connect to IoT Edge
        await client.connect()

        logger.info("Analytic module connected to IoT Edge.")

        # Set the message handler
        client.on_message_received = receive_message_handler

        # Keep the module alive
        while True:
            await asyncio.sleep(1)

    except KeyboardInterrupt:
        logger.info("Analytic module stopped by the user.")
    except Exception as ex:
        logger.error("Error: %s", ex)

if __name__ == "__main__":
    asyncio.run(main())
