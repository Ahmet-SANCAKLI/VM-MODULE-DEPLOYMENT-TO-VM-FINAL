import asyncio
import json
import time
import logging
from azure.iot.device.aio import IoTHubModuleClient
from pyModbusTCP.client import ModbusClient

# IoT Hub bağlantısı için ModuleClient oluşturun
client = IoTHubModuleClient.create_from_edge_environment()

# Logger'ı yapılandırın
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Modbus Sunucu IP adresini ve Port numarasını burada yapılandırın
modbus_server_ip = "10.0.0.4"
modbus_server_port = 1502

#Modbus Sunucu holding register adreslerini burada yapılandırın
roll_register = 0
pitch_register = 1
heading_register = 2
gForcemagnitude_register = 3
east_offset_register = 4
north_offset_register = 5
total_offset_register = 6

async def main():
    try:
        # ModuleClient'ı başlatın
        await client.connect()

        # Modbus Client oluşturun ve Modbus sunucusuna bağlanın
        modbus_client = ModbusClient()
        modbus_client.host = modbus_server_ip  # Modbus Server'ın IP adresini "localhost" olarak ayarlayın
        modbus_client.port = modbus_server_port         # Modbus Server'ın Port numarasını 1502 olarak ayarlayın
        if not modbus_client.open():
            logger.error("Unable to connect Modbus Server!")
            return
        while True:
            try:
                # Holding register adreslerinden verileri alın
                roll_data = modbus_client.read_holding_registers(roll_register)
                pitch_data = modbus_client.read_holding_registers(pitch_register)
                heading_data = modbus_client.read_holding_registers(heading_register)
                gForcemagnitude_data = modbus_client.read_holding_registers(gForcemagnitude_register)
                east_offset_data = modbus_client.read_holding_registers(east_offset_register)
                north_offset_data = modbus_client.read_holding_registers(north_offset_register)
                total_offset_data = modbus_client.read_holding_registers(total_offset_register)

                # Verileri okuyup, JSON formatında düzenleyin
                if all([roll_data, pitch_data, heading_data, gForcemagnitude_data, east_offset_data, north_offset_data, total_offset_data]):
                    roll = roll_data[0] / 1000.0
                    pitch = pitch_data[0] / 1000.0
                    heading = heading_data[0] / 100.0
                    gForcemagnitude = gForcemagnitude_data[0] / 100.0
                    east_offset = east_offset_data[0] / 100.0
                    north_offset = north_offset_data[0] / 100.0
                    total_offset = total_offset_data[0] / 100.0

                    data = {
                        "roll": roll,
                        "pitch": pitch,
                        "heading": heading,
                        "gForcemagnitude": gForcemagnitude,
                        "east_offset": east_offset,
                        "north_offset": north_offset,
                        "total_offset": total_offset
                    }
                    message = {
                        "deviceId": "ModbusClientModule",
                        "data": data,
                        "timestamp": int(time.time())
                    }

                    # Veriyi JSON formatına çevirin
                    message_json = json.dumps(message)

                    # IoT Edge Hub üzerindeki Analytics Modülüne gönderin
                    response = await client.send_message_to_output(message_json, "output1")
                    logger.info("Message has been sent to Analytic Module. response: %s", response)

                    # Log verisini görüntüleyin
                    logger.info("Message sent: %s", message_json)
                else:
                    logger.error("Unable to Convert Data")
            except Exception as ex:
                logger.error("An error occurred on reading data on Modbus Server")
            
            # Mesaj gönderdikten sonra belirli bir süre bekleyin
            await asyncio.sleep(10)  # Örnek olarak 5 saniye bekleyin
    
    except Exception as ex:
        logger.error("An error occured on Modbus Client Module: %s", ex)

if __name__ == "__main__":
    asyncio.run(main())
