using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace IotDeviceLibrary.BME280
{
    //https://github.com/adafruit/Adafruit_BME280_Library
    public class BME280 : Device, IBME280
    {

        // private byte _i2Caddr;
        //private int _sensorId;
        private uint _tFine;
        private BME280CalibrationData _calibrationData;
        private readonly string I2CControllerName = "I2C1";


        /*=========================================================================
            I2C ADDRESS/BITS
            -----------------------------------------------------------------------*/
        private readonly byte _bme280Address = (0x77);
        /*=========================================================================*/

        /*=========================================================================
            REGISTERS
            -----------------------------------------------------------------------*/
        private enum Registers : byte
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

        public BME280(byte address = 0x77)
        {
            _bme280Address = address;
            _calibrationData = new BME280CalibrationData();
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

            /* SPI
            var s = new SpiConnectionSettings(0)
            {
                Mode = SpiMode.Mode0,
            };
            string args = SpiDevice.GetDeviceSelector("SPI0");
            var deviceInformation = DeviceInformation.FindAllAsync(args);*/


        }

        public override void Begin()
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
            @brief  Reads the factory-set coefficients 
        */
        /**************************************************************************/
        private void ReadCoefficients()
        {
            _calibrationData.DigT1 = Read16((byte)Registers.RegisterDigT1);
            _calibrationData.DigT2 = (short)Read16((byte)Registers.RegisterDigT2);
            _calibrationData.DigT3 = (short)Read16((byte)Registers.RegisterDigT3);

            _calibrationData.DigP1 = Read16((byte)Registers.RegisterDigP1);
            _calibrationData.DigP2 = (short)Read16((byte)Registers.RegisterDigP2);
            _calibrationData.DigP3 = (short)Read16((byte)Registers.RegisterDigP3);
            _calibrationData.DigP4 = (short)Read16((byte)Registers.RegisterDigP4);
            _calibrationData.DigP5 = (short)Read16((byte)Registers.RegisterDigP5);
            _calibrationData.DigP6 = (short)Read16((byte)Registers.RegisterDigP6);
            _calibrationData.DigP7 = (short)Read16((byte)Registers.RegisterDigP7);
            _calibrationData.DigP8 = (short)Read16((byte)Registers.RegisterDigP8);
            _calibrationData.DigP9 = (short)Read16((byte)Registers.RegisterDigP9);

            _calibrationData.DigH1 = Read8((byte)Registers.RegisterDigH1);
            _calibrationData.DigH2 = (short)Read16((byte)Registers.RegisterDigH2);
            _calibrationData.DigH3 = Read8((byte)Registers.RegisterDigH3);
            _calibrationData.DigH4 = (byte)((Read8((byte)Registers.RegisterDigH4) << 4) | (Read8((byte)Registers.RegisterDigH4 + 1) & 0xF));
            _calibrationData.DigH5 = (byte)((Read8((byte)Registers.RegisterDigH5 + 1) << 4) | (Read8((byte)Registers.RegisterDigH5) >> 4));
            _calibrationData.DigH6 = (byte)Read8((byte)Registers.RegisterDigH6);
        }

        /**************************************************************************/
        /*!

        */
        /**************************************************************************/
        public double ReadTemperature()
        {
            uint var1, var2;

            uint adc_T = Read24((byte)Registers.RegisterTempdata);
            adc_T >>= 4;

            var1 = ((((adc_T >> 3) - _calibrationData.DigT1 << 1)) * ((uint)_calibrationData.DigT2)) >> 11;

            var2 = (((((adc_T >> 4) - (_calibrationData.DigT1)) *
                   ((adc_T >> 4) - (_calibrationData.DigT1))) >> 12) *
                 ((uint)_calibrationData.DigT3)) >> 14;

            _tFine = var1 + var2;

            float T = (_tFine * 5 + 128) >> 8;
            return T / 100;
        }

        /**************************************************************************/
        /*!

        */
        /**************************************************************************/
        public float readPressure()
        {
            ulong var1, var2, p;

            ReadTemperature(); // must be done first to get t_fine

            uint adc_P = Read24((byte)Registers.RegisterPressuredata);
            adc_P >>= 4;

            var1 = ((ulong)_tFine) - 128000;
            var2 = var1 * var1 * (ulong)_calibrationData.DigP6;
            var2 = var2 + ((var1 * (ulong)_calibrationData.DigP5) << 17);
            var2 = var2 + (((ulong)_calibrationData.DigP4) << 35);
            var1 = ((var1 * var1 * (ulong)_calibrationData.DigP3) >> 8) +
              ((var1 * (ulong)_calibrationData.DigP2) << 12);
            var1 = (((((ulong)1) << 47) + var1)) * ((ulong)_calibrationData.DigP1) >> 33;

            if (var1 == 0)
            {
                return 0;  // avoid exception caused by division by zero
            }
            p = 1048576 - adc_P;
            p = (((p << 31) - var2) * 3125) / var1;
            var1 = (((ulong)_calibrationData.DigP9) * (p >> 13) * (p >> 13)) >> 25;
            var2 = (((ulong)_calibrationData.DigP8) * p) >> 19;

            p = ((p + var1 + var2) >> 8) + (((ulong)_calibrationData.DigP7) << 4);
            return (float)p / 256;
        }

        /**************************************************************************/
        /*!

        */
        /**************************************************************************/
        public double ReadHumidity()
        {
            ReadTemperature(); // must be done first to get t_fine

            int adc_H = Read16((byte)Registers.RegisterControlhumid);

            uint v_x1_u32r;

            v_x1_u32r = (_tFine - ((uint)76800));

            v_x1_u32r = (uint)(((((adc_H << 14) - (((uint)_calibrationData.DigH4) << 20) -
                    (((uint)_calibrationData.DigH5) * v_x1_u32r)) + ((uint)16384)) >> 15) *
                     (((((((v_x1_u32r * ((uint)_calibrationData.DigH6)) >> 10) *
                      (((v_x1_u32r * ((uint)_calibrationData.DigH3)) >> 11) + ((uint)32768))) >> 10) +
                    ((uint)2097152)) * ((uint)_calibrationData.DigH2) + 8192) >> 14));

            v_x1_u32r = (v_x1_u32r - (((((v_x1_u32r >> 15) * (v_x1_u32r >> 15)) >> 7) *
                           ((uint)_calibrationData.DigH1)) >> 4));

            v_x1_u32r = (v_x1_u32r < 0) ? 0 : v_x1_u32r;
            v_x1_u32r = (v_x1_u32r > 419430400) ? 419430400 : v_x1_u32r;
            double h = (v_x1_u32r >> 12);
            return h / 1024.0;
        }

        /// <summary>
        ///  Calculates the altitude (in meters) from the specified atmospheric pressure(in hPa), and sea-level pressure(in hPa).
        /// </summary>
        /// <param name="seaLevel" > 
        ///   Sea-level pressure in hPa
        /// </param>
        /// <returns>
        ///   Atmospheric pressure in hPa
        /// </returns>
        public double ReadAltitude(double seaLevel)
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
