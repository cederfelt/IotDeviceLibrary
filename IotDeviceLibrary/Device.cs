using IotDeviceLibrary.TCS34725;
using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace IotDeviceLibrary
{
    public abstract class Device : IDevice
    {
        protected I2cDevice I2CDevice;

        public abstract Task Initialize();
        public abstract void Begin();

        protected bool initialised = false;
        public bool Initilized { get { return initialised; } }

        //Method to read an 8-bit value from a register
        protected virtual byte Read8(byte register)
        {
            byte value = 0;
            byte[] writeBuffer = new byte[] { 0x00 };
            byte[] readBuffer = new byte[] { 0x00 };

            writeBuffer[0] = register;

            I2CDevice.WriteRead(writeBuffer, readBuffer);
            value = readBuffer[0];
            return value;
        }

        /**************************************************************************/
        /*! 
            @brief  Reads a 16 bit value over I2C 
        */
        /**************************************************************************/
        protected ushort Read16(byte register)
        {

            ushort value;

            byte[] writeBuffer = new byte[] { 0x00 };
            byte[] readBuffer = new byte[] { 0x00, 0x00 };

            writeBuffer[0] = register;

            I2CDevice.WriteRead(writeBuffer, readBuffer);
            int h = readBuffer[1] << 8;
            int l = readBuffer[0];
            value = (ushort)(h + l);

            //TODO MIGHT SUPPORT SPI LATER
            /*
              } else { 
                if (_sck == -1) 
                  SPI.beginTransaction(SPISettings(500000, MSBFIRST, SPI_MODE0)); 
                digitalWrite(_cs, LOW); 
                spixfer(reg | 0x80); // read, bit 7 high 
                value = (spixfer(0) << 8) | spixfer(0); 
                digitalWrite(_cs, HIGH); 
                if (_sck == -1) 
                 SPI.endTransaction();              // release the SPI bus 
              } */

            return value;
        }

        /**************************************************************************/
        /*! 
            @brief  Reads a 24 bit value over I2C 
        */
        /**************************************************************************/
        protected uint Read24(byte register)
        {
            uint value;

            byte[] writeBuffer = new byte[] { 0x00 };
            byte[] readBuffer = new byte[] { 0x00, 0x00 };

            writeBuffer[0] = register;

            I2CDevice.WriteRead(writeBuffer, readBuffer);
            value = readBuffer[2];
            value <<= 8;
            value = readBuffer[1];
            value <<= 8;
            value = readBuffer[0];

            //TODO SPI
            /*} else { 
              if (_sck == -1) 
                SPI.beginTransaction(SPISettings(500000, MSBFIRST, SPI_MODE0)); 
              digitalWrite(_cs, LOW); 
              spixfer(reg | 0x80); // read, bit 7 high 

              value = spixfer(0); 
              value <<= 8; 
              value |= spixfer(0); 
              value <<= 8; 
              value |= spixfer(0); 


              digitalWrite(_cs, HIGH); 
              if (_sck == -1) 
                SPI.endTransaction();              // release the SPI bus 
            } */
            return value;
        }


        /* SPI
        public byte ReadByte(byte byteToSend)
        {

            byte[] sendbuffer = new byte[] { byteToSend | 0x80 }; //set the first bit to 1

            byte[] readBuffer = new byte[1];

            _spi_Bme280.TransferFullDuplex(sendBuffer, readBuffer); //functionname are correct in my actual code. This is just as I remember it 

            return readBuffer[0];
        }*/
    }
}
