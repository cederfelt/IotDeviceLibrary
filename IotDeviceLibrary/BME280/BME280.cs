using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace IotDeviceLibrary.BME280
{
    //https://github.com/adafruit/Adafruit_BME280_Library
    class Bme280 : Device, IBME280
    {

        byte _i2Caddr;
        int _sensorId;
        UInt32 _tFine;
        Bme280CalibData _bme280Calib;
        private const string I2CControllerName = "I2C1";

        /*=========================================================================
            I2C ADDRESS/BITS
            -----------------------------------------------------------------------*/
        private byte _bme280Address = (0x77);
        /*=========================================================================*/

        /*=========================================================================
            REGISTERS
            -----------------------------------------------------------------------*/
        enum Registers : byte
        {
            RegisterDigT1 = 0x88,
            RegisterDigT2 = 0x8A,
            RegisterDigT3 = 0x8C,

            RegisterDigP1 = 0x8E,
            RegisterDigP2 = 0x90,
            RegisterDigP3 = 0x92,
            RegisterDigP4 = 0x94,
            RegisterDigP5 = 0x96,
            RegisterDigP6 = 0x98,
            RegisterDigP7 = 0x9A,
            RegisterDigP8 = 0x9C,
            RegisterDigP9 = 0x9E,

            RegisterDigH1 = 0xA1,
            RegisterDigH2 = 0xE1,
            RegisterDigH3 = 0xE3,
            RegisterDigH4 = 0xE4,
            RegisterDigH5 = 0xE5,
            RegisterDigH6 = 0xE7,

            RegisterChipid = 0xD0,
            RegisterVersion = 0xD1,
            RegisterSoftreset = 0xE0,

            RegisterCal26 = 0xE1,  // R calibration stored in 0xE1-0xF0

            RegisterControlhumid = 0xF2,
            RegisterControl = 0xF4,
            RegisterConfig = 0xF5,
            RegisterPressuredata = 0xF7,
            RegisterTempdata = 0xFA,
            RegisterHumiddata = 0xFD,
        };

        public struct Bme280CalibData
        {
            //    public /*uint16_t*/ UInt16 dig_T1;
            public /*uint16_t*/ short DigT1;
            public short DigT2;
            public short DigT3;

            //public /*uint16_t*/ UInt16 dig_P1;
            public /*uint16_t*/ short DigP1;
            public short DigP2;
            public short DigP3;
            public short DigP4;
            public short DigP5;
            public short DigP6;
            public short DigP7;
            public short DigP8;
            public short DigP9;

            // public /*uint8_t*/ UInt16 dig_H1;
            public /*uint8_t*/ short DigH1;
            public short DigH2;
            // public /*uint8_t*/ UInt16 dig_H3;
            public /*uint8_t*/ short DigH3;
            public byte DigH4;
            public byte DigH5;
            public byte DigH6;
        }

        public override async Task Initialize()
        {
            Debug.WriteLine("BME280 initialized");
            try
            {
                I2cConnectionSettings settings = new I2cConnectionSettings(_bme280Address);

                settings.BusSpeed = I2cBusSpeed.FastMode;

                string aqs = I2cDevice.GetDeviceSelector(I2CControllerName);

                DeviceInformationCollection dis = await DeviceInformation.FindAllAsync(aqs);

                I2CDevice = await I2cDevice.FromIdAsync(dis[0].Id, settings);

                if (I2CDevice == null)
                {
                    Debug.WriteLine("Device not found");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message + "\n" + e.StackTrace);
                throw;
            }
        }

        public override async Task Begin()
        {
            Debug.WriteLine("BME280::BEGIN");
            byte[] writeBuffer = new byte[] { (byte)Registers.RegisterChipid };
            byte[] readBuffer = new byte[] { 0xFF };

            I2CDevice.WriteRead(writeBuffer, readBuffer);
            Debug.WriteLine("BME280 Signature: " + readBuffer[0].ToString());

            /*
            if (readBuffer[0] != BME280_Signature)
            {
                {
                    Debug.WriteLine("BME280::Begin Signature Mismatch.");
                    return;
                }
            }*/
            initialised = true;

            //Read the coefficients table
            // _calibrationData = await ReadCoefficeints();

            //Write control register
            // await WriteControlRegister();
        }

        /**************************************************************************/
        /*! 
            @brief  Reads a 16 bit value over I2C 
        */
        /**************************************************************************/
        short Read16(byte register) 
        {
            UInt16 value;

            byte[] writeBuffer = new byte[] { 0x00 };
            byte[] readBuffer = new byte[] { 0x00, 0x00 };

            writeBuffer[0] = register;

            I2CDevice.WriteRead(writeBuffer, readBuffer);
            int h = readBuffer[1] << 8;
            int l = readBuffer[0];
            value = (UInt16)(h + l);

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


            return (short)value;
        }

        /**************************************************************************/
        /*! 
            @brief  Reads a 24 bit value over I2C 
        */
        /**************************************************************************/
        UInt32 read24(byte register)
        {
            UInt32 value;

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

        /**************************************************************************/
        /*! 
            @brief  Reads the factory-set coefficients 
        */
        /**************************************************************************/
        private void ReadCoefficients()
        {
            _bme280Calib.DigT1 = Read16((byte)Registers.RegisterDigT1);
            _bme280Calib.DigT2 = Read16((byte)Registers.RegisterDigT2);
            _bme280Calib.DigT3 = Read16((byte)Registers.RegisterDigT3);

            _bme280Calib.DigP1 = Read16((byte)Registers.RegisterDigP1);
            _bme280Calib.DigP2 = Read16((byte)Registers.RegisterDigP2);
            _bme280Calib.DigP3 = Read16((byte)Registers.RegisterDigP3);
            _bme280Calib.DigP4 = Read16((byte)Registers.RegisterDigP4);
            _bme280Calib.DigP5 = Read16((byte)Registers.RegisterDigP5);
            _bme280Calib.DigP6 = Read16((byte)Registers.RegisterDigP6);
            _bme280Calib.DigP7 = Read16((byte)Registers.RegisterDigP7);
            _bme280Calib.DigP8 = Read16((byte)Registers.RegisterDigP8);
            _bme280Calib.DigP9 = Read16((byte)Registers.RegisterDigP9);

            _bme280Calib.DigH1 = ReadByte((byte)Registers.RegisterDigH1);
            _bme280Calib.DigH2 = Read16((byte)Registers.RegisterDigH2);
            _bme280Calib.DigH3 = ReadByte((byte)Registers.RegisterDigH3);
            _bme280Calib.DigH4 = (byte)((ReadByte((byte)Registers.RegisterDigH4) << 4) | (ReadByte((byte)Registers.RegisterDigH4 + 1) & 0xF));
            _bme280Calib.DigH5 = (byte)((ReadByte((byte)Registers.RegisterDigH5 + 1) << 4) | (ReadByte((byte)Registers.RegisterDigH5) >> 4));
            _bme280Calib.DigH6 = (byte)ReadByte((byte)Registers.RegisterDigH6);
        }

        /**************************************************************************/
        /*!

        */
        /**************************************************************************/
        double ReadTemperature()
        {
            UInt32 var1, var2;

            UInt32 adc_T = read24((byte)Registers.RegisterTempdata);
            adc_T >>= 4;

            var1 = ((((adc_T >> 3) - (UInt32)_bme280Calib.DigT1 << 1)) * ((UInt32)_bme280Calib.DigT2)) >> 11;

            var2 = (((((adc_T >> 4) - ((UInt32)_bme280Calib.DigT1)) *
                   ((adc_T >> 4) - ((UInt32)_bme280Calib.DigT1))) >> 12) *
                 ((UInt32)_bme280Calib.DigT3)) >> 14;

            _tFine = var1 + var2;

            float T = (_tFine * 5 + 128) >> 8;
            return T / 100;
        }

        /**************************************************************************/
        /*!

        */
        /**************************************************************************/
        float readPressure()
        {
            UInt64 var1, var2, p;

            ReadTemperature(); // must be done first to get t_fine

            UInt32 adc_P = read24((byte)Registers.RegisterPressuredata);
            adc_P >>= 4;

            var1 = ((UInt64)_tFine) - 128000;
            var2 = var1 * var1 * (UInt64)_bme280Calib.DigP6;
            var2 = var2 + ((var1 * (UInt64)_bme280Calib.DigP5) << 17);
            var2 = var2 + (((UInt64)_bme280Calib.DigP4) << 35);
            var1 = ((var1 * var1 * (UInt64)_bme280Calib.DigP3) >> 8) +
              ((var1 * (UInt64)_bme280Calib.DigP2) << 12);
            var1 = (((((UInt64)1) << 47) + var1)) * ((UInt64)_bme280Calib.DigP1) >> 33;

            if (var1 == 0)
            {
                return 0;  // avoid exception caused by division by zero
            }
            p = 1048576 - adc_P;
            p = (((p << 31) - var2) * 3125) / var1;
            var1 = (((UInt64)_bme280Calib.DigP9) * (p >> 13) * (p >> 13)) >> 25;
            var2 = (((UInt64)_bme280Calib.DigP8) * p) >> 19;

            p = ((p + var1 + var2) >> 8) + (((UInt64)_bme280Calib.DigP7) << 4);
            return (float)p / 256;
        }

        /**************************************************************************/
        /*!

        */
        /**************************************************************************/
        double ReadHumidity()
        {
            ReadTemperature(); // must be done first to get t_fine

            int adc_H = Read16((byte)Registers.RegisterControlhumid);

            UInt32 v_x1_u32r;

            v_x1_u32r = (_tFine - ((UInt32)76800));

            v_x1_u32r = (UInt32)(((((adc_H << 14) - (((UInt32)_bme280Calib.DigH4) << 20) -
                    (((UInt32)_bme280Calib.DigH5) * v_x1_u32r)) + ((UInt32)16384)) >> 15) *
                     (((((((v_x1_u32r * ((UInt32)_bme280Calib.DigH6)) >> 10) *
                      (((v_x1_u32r * ((UInt32)_bme280Calib.DigH3)) >> 11) + ((UInt32)32768))) >> 10) +
                    ((UInt32)2097152)) * ((UInt32)_bme280Calib.DigH2) + 8192) >> 14));

            v_x1_u32r = (v_x1_u32r - (((((v_x1_u32r >> 15) * (v_x1_u32r >> 15)) >> 7) *
                           ((UInt32)_bme280Calib.DigH1)) >> 4));

            v_x1_u32r = (v_x1_u32r < 0) ? 0 : v_x1_u32r;
            v_x1_u32r = (v_x1_u32r > 419430400) ? 419430400 : v_x1_u32r;
            double h = (v_x1_u32r >> 12);
            return h / 1024.0;
        }

        /**************************************************************************/
        /*!
            Calculates the altitude (in meters) from the specified atmospheric
            pressure (in hPa), and sea-level pressure (in hPa).

            @param  seaLevel      Sea-level pressure in hPa
            @param  atmospheric   Atmospheric pressure in hPa
        */
        /**************************************************************************/
        double ReadAltitude(double seaLevel)
        {
            // Equation taken from BMP180 datasheet (page 16):
            //  http://www.adafruit.com/datasheets/BST-BMP180-DS000-09.pdf

            // Note that using the equation from wikipedia can give bad results
            // at high altitude.  See this thread for more information:
            //  http://forums.adafruit.com/viewtopic.php?f=22&t=58064

            double atmospheric = readPressure() / 100.0;
            return 44330.0 * (1.0 - Math.Pow(atmospheric / seaLevel, 0.1903));
        }


    }
}
