using System;
using System.Devices.I2c;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Iot.Device
{
    //https://github.com/adafruit/Adafruit_BME280_Library
    public class BME280 : IBME280
    {
        private uint TFine;
        private BME280CalibrationData CalibrationData;
        private readonly string I2CControllerName = "I2C1";
        protected readonly byte Address;
        protected readonly byte Signature;
        protected I2cDevice I2CDevice;
        protected bool initialised = false;
        public bool Initilized { get { return initialised; } }


        /// <summary>
        /// Registers
        /// </summary>
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

        public BME280(byte address = 0x77, byte signature = 0x60)
        {
            CalibrationData = new BME280CalibrationData();
        }

        public void Initialize()
        {
            Debug.WriteLine("BME280 initialized");
            try
            {
                I2cConnectionSettings settings = new I2cConnectionSettings(Address);

                I2CDevice = new UnixI2cDevice(settings);

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

        public void Begin()
        {
            Debug.WriteLine("BME280::BEGIN");
            byte[] writeBuffer = new byte[] { (byte)Registers.RegisterChipid };
            byte[] readBuffer = new byte[] { 0xFF };

            I2CDevice.WriteRead(writeBuffer, readBuffer);
            Debug.WriteLine("BME280 Signature: " + readBuffer[0].ToString());

            if (readBuffer[0] != Signature)
            {
                {
                    Debug.WriteLine("BME280::Begin Signature Mismatch.");
                    return;
                }
            }
            initialised = true;
        }

        /// <summary>
        /// Reads the factory-set coefficients 
        /// </summary>
        private void ReadCoefficients()
        {
            CalibrationData.DigT1 = Read16((byte)Registers.RegisterDigT1);
            CalibrationData.DigT2 = (short)Read16((byte)Registers.RegisterDigT2);
            CalibrationData.DigT3 = (short)Read16((byte)Registers.RegisterDigT3);

            CalibrationData.DigP1 = Read16((byte)Registers.RegisterDigP1);
            CalibrationData.DigP2 = (short)Read16((byte)Registers.RegisterDigP2);
            CalibrationData.DigP3 = (short)Read16((byte)Registers.RegisterDigP3);
            CalibrationData.DigP4 = (short)Read16((byte)Registers.RegisterDigP4);
            CalibrationData.DigP5 = (short)Read16((byte)Registers.RegisterDigP5);
            CalibrationData.DigP6 = (short)Read16((byte)Registers.RegisterDigP6);
            CalibrationData.DigP7 = (short)Read16((byte)Registers.RegisterDigP7);
            CalibrationData.DigP8 = (short)Read16((byte)Registers.RegisterDigP8);
            CalibrationData.DigP9 = (short)Read16((byte)Registers.RegisterDigP9);

            CalibrationData.DigH1 = Read8((byte)Registers.RegisterDigH1);
            CalibrationData.DigH2 = (short)Read16((byte)Registers.RegisterDigH2);
            CalibrationData.DigH3 = Read8((byte)Registers.RegisterDigH3);
            CalibrationData.DigH4 = (byte)((Read8((byte)Registers.RegisterDigH4) << 4) | (Read8((byte)Registers.RegisterDigH4 + 1) & 0xF));
            CalibrationData.DigH5 = (byte)((Read8((byte)Registers.RegisterDigH5 + 1) << 4) | (Read8((byte)Registers.RegisterDigH5) >> 4));
            CalibrationData.DigH6 = (byte)Read8((byte)Registers.RegisterDigH6);
        }

        public double ReadTemperature()
        {
            if (!initialised) { Begin(); }

            uint var1, var2;

            uint adc_T = Read24((byte)Registers.RegisterTempdata);
            adc_T >>= 4;

            var1 = ((((adc_T >> 3) - CalibrationData.DigT1 << 1)) * ((uint)CalibrationData.DigT2)) >> 11;

            var2 = (((((adc_T >> 4) - (CalibrationData.DigT1)) *
                   ((adc_T >> 4) - (CalibrationData.DigT1))) >> 12) *
                 ((uint)CalibrationData.DigT3)) >> 14;

            TFine = var1 + var2;

            float T = (TFine * 5 + 128) >> 8;
            return T / 100;
        }
        
        public double readPressure()
        {
            ulong var1, var2, p;

            ReadTemperature(); // must be done first to get t_fine

            uint adc_P = Read24((byte)Registers.RegisterPressuredata);
            adc_P >>= 4;

            var1 = ((ulong)TFine) - 128000;
            var2 = var1 * var1 * (ulong)CalibrationData.DigP6;
            var2 = var2 + ((var1 * (ulong)CalibrationData.DigP5) << 17);
            var2 = var2 + (((ulong)CalibrationData.DigP4) << 35);
            var1 = ((var1 * var1 * (ulong)CalibrationData.DigP3) >> 8) +
              ((var1 * (ulong)CalibrationData.DigP2) << 12);
            var1 = (((((ulong)1) << 47) + var1)) * ((ulong)CalibrationData.DigP1) >> 33;

            if (var1 == 0)
            {
                return 0;  // avoid exception caused by division by zero
            }
            p = 1048576 - adc_P;
            p = (((p << 31) - var2) * 3125) / var1;
            var1 = (((ulong)CalibrationData.DigP9) * (p >> 13) * (p >> 13)) >> 25;
            var2 = (((ulong)CalibrationData.DigP8) * p) >> 19;

            p = ((p + var1 + var2) >> 8) + (((ulong)CalibrationData.DigP7) << 4);
            return (float)p / 256;
        }

        public double ReadHumidity()
        {
            ReadTemperature(); // must be done first to get t_fine

            int adc_H = Read16((byte)Registers.RegisterControlhumid);

            uint v_x1_u32r;

            v_x1_u32r = (TFine - ((uint)76800));

            v_x1_u32r = (uint)(((((adc_H << 14) - (((uint)CalibrationData.DigH4) << 20) -
                    (((uint)CalibrationData.DigH5) * v_x1_u32r)) + ((uint)16384)) >> 15) *
                     (((((((v_x1_u32r * ((uint)CalibrationData.DigH6)) >> 10) *
                      (((v_x1_u32r * ((uint)CalibrationData.DigH3)) >> 11) + ((uint)32768))) >> 10) +
                    ((uint)2097152)) * ((uint)CalibrationData.DigH2) + 8192) >> 14));

            v_x1_u32r = (v_x1_u32r - (((((v_x1_u32r >> 15) * (v_x1_u32r >> 15)) >> 7) *
                           ((uint)CalibrationData.DigH1)) >> 4));

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

        /// <summary>
        /// Method to read an 8-bit value from a register
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a 16 bit value over I2C 
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
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

            return value;
        }

        /// <summary>
        ///  @brief  Reads a 24 bit value over I2C 
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
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

            return value;
        }
    }
}
