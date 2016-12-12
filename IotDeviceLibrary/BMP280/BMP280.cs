using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace IotDeviceLibrary.BMP280
{
    public class BMP280 : Device, IBMP280
    {
        //The BMP280 register addresses according the the datasheet: http://www.adafruit.com/datasheets/BST-BMP280-DS001-11.pdf

        //t_fine carries fine temperature as global value
        private int t_fine = int.MinValue;
        private enum Registers : byte
        {
            REGISTER_DIG_T1 = 0x88,
            REGISTER_DIG_T2 = 0x8A,
            REGISTER_DIG_T3 = 0x8C,

            REGISTER_DIG_P1 = 0x8E,
            REGISTER_DIG_P2 = 0x90,
            REGISTER_DIG_P3 = 0x92,
            REGISTER_DIG_P4 = 0x94,
            REGISTER_DIG_P5 = 0x96,
            REGISTER_DIG_P6 = 0x98,
            REGISTER_DIG_P7 = 0x9A,
            REGISTER_DIG_P8 = 0x9C,
            REGISTER_DIG_P9 = 0x9E,

            REGISTER_CHIPID = 0xD0,
            REGISTER_VERSION = 0xD1,
            REGISTER_SOFTRESET = 0xE0,

            REGISTER_CAL26 = 0xE1,  // R calibration stored in 0xE1-0xF0

            REGISTER_CONTROLHUMID = 0xF2,
            REGISTER_CONTROL = 0xF4,
            REGISTER_CONFIG = 0xF5,

            REGISTER_PRESSUREDATA_MSB = 0xF7,
            REGISTER_PRESSUREDATA_LSB = 0xF8,
            REGISTER_PRESSUREDATA_XLSB = 0xF9, // bits <7:4>

            REGISTER_TEMPDATA_MSB = 0xFA,
            REGISTER_TEMPDATA_LSB = 0xFB,
            REGISTER_TEMPDATA_XLSB = 0xFC, // bits <7:4>

            REGISTER_HUMIDDATA_MSB = 0xFD,
            REGISTER_HUMIDDATA_LSB = 0xFE,
        };

        private const string I2CControllerName = "I2C1";

        private BMP280CalibrationData _calibrationData;

        public BMP280(byte address = 0x77, byte signature = 0x58) : base(address, signature)
        {
        }

        public override async Task Initialize()
        {
            Debug.WriteLine("BMP280 initialized");
            try
            {
                I2cConnectionSettings settings = new I2cConnectionSettings(Address);

                settings.BusSpeed = I2cBusSpeed.FastMode;

                string aqs = I2cDevice.GetDeviceSelector(I2CControllerName);

                DeviceInformationCollection dic = await DeviceInformation.FindAllAsync(aqs);

                I2CDevice = await I2cDevice.FromIdAsync(dic[0].Id, settings);

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

        public override void Begin()
        {
            Debug.WriteLine("BMP280::BEGIN");
            byte[] writeBuffer = new byte[] { (byte)Registers.REGISTER_CHIPID };
            byte[] readBuffer = new byte[] { 0xFF };

            I2CDevice.WriteRead(writeBuffer, readBuffer);
            Debug.WriteLine("BMP280 Signature: " + readBuffer[0].ToString());

            if (readBuffer[0] != Signature)
            {
                {
                    Debug.WriteLine("BMP280::Begin Signature Mismatch.");
                    return;
                }
            }
            initialised = true;

            //Read the coefficients table
            _calibrationData = ReadCoefficients();

            //Write control register
            WriteControlRegister();

            //Write humidity control register
            WriteControlRegisterHumidity();
        }

        //Method to write 0x03 to the humidity control register
        private void WriteControlRegisterHumidity()
        {
            byte[] writeBuffer = new byte[] { (byte)Registers.REGISTER_CONTROLHUMID, 0x03 };
            I2CDevice.Write(writeBuffer);
            return;
        }

        //Method to write 0x3F to the control register
        private void WriteControlRegister()
        {
            byte[] writeBuffer = new byte[] { (byte)Registers.REGISTER_CONTROL, 0x3F };
            I2CDevice.Write(writeBuffer);
            return;
        }

        public float ReadTemperature()
        {
            //Make sure the I2C device is initialized
            if (!initialised) Begin();

            //Read the MSB, LSB and bits 7:4 (XLSB) of the temperature from the BMP280 registers
            byte tmsb = Read8((byte)Registers.REGISTER_TEMPDATA_MSB);
            byte tlsb = Read8((byte)Registers.REGISTER_TEMPDATA_LSB);
            byte txlsb = Read8((byte)Registers.REGISTER_TEMPDATA_XLSB); // bits 7:4

            //Combine the values into a 32-bit integer
            int t = (tmsb << 12) + (tlsb << 4) + (txlsb >> 4);

            //Convert the raw value to the temperature in degC
            double temp = BMP280_compensate_T_double(t);

            //Return the temperature as a float value
            return (float)temp;
        }

        public float ReadPreasure()
        {
            //Make sure the I2C device is initialized
            if (!initialised) Begin();

            //Read the temperature first to load the t_fine value for compensation
            if (t_fine == int.MinValue)
            {
                ReadTemperature();
            }

            //Read the MSB, LSB and bits 7:4 (XLSB) of the pressure from the BMP280 registers
            byte tmsb = Read8((byte)Registers.REGISTER_PRESSUREDATA_MSB);
            byte tlsb = Read8((byte)Registers.REGISTER_PRESSUREDATA_LSB);
            byte txlsb = Read8((byte)Registers.REGISTER_PRESSUREDATA_XLSB); // bits 7:4

            //Combine the values into a 32-bit integer
            int t = (tmsb << 12) + (tlsb << 4) + (txlsb >> 4);

            //Convert the raw value to the pressure in Pa
            long pres = BMP280_compensate_P_Int64(t);

            //Return the temperature as a float value
            return ((float)pres) / 256;
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
        public float ReadAltitude(float seaLevel)
        {
            //Make sure the I2C device is initialized
            if (!initialised) Begin();

            //Read the pressure first
            float pressure = ReadPreasure();
            //Convert the pressure to Hectopascals(hPa)
            pressure /= 100;

            //Calculate and return the altitude using the international barometric formula
            return 44330.0f * (1.0f - (float)Math.Pow((pressure / seaLevel), 0.1903f));
        }

        //Method to read the caliberation data from the registers
        private BMP280CalibrationData ReadCoefficients()
        {
            // 16 bit calibration data is stored as Little Endian, the helper method will do the byte swap.
            _calibrationData = new BMP280CalibrationData();

            // Read temperature calibration data
            _calibrationData.DigT1 = Read16((byte)Registers.REGISTER_DIG_T1);
            _calibrationData.DigT2 = (short)Read16((byte)Registers.REGISTER_DIG_T2);
            _calibrationData.DigT3 = (short)Read16((byte)Registers.REGISTER_DIG_T3);

            // Read presure calibration data
            _calibrationData.DigP1 = Read16((byte)Registers.REGISTER_DIG_P1);
            _calibrationData.DigP2 = (short)Read16((byte)Registers.REGISTER_DIG_P2);
            _calibrationData.DigP3 = (short)Read16((byte)Registers.REGISTER_DIG_P3);
            _calibrationData.DigP4 = (short)Read16((byte)Registers.REGISTER_DIG_P4);
            _calibrationData.DigP5 = (short)Read16((byte)Registers.REGISTER_DIG_P5);
            _calibrationData.DigP6 = (short)Read16((byte)Registers.REGISTER_DIG_P6);
            _calibrationData.DigP7 = (short)Read16((byte)Registers.REGISTER_DIG_P7);
            _calibrationData.DigP8 = (short)Read16((byte)Registers.REGISTER_DIG_P8);
            _calibrationData.DigP9 = (short)Read16((byte)Registers.REGISTER_DIG_P9);

            return _calibrationData;
        }

        //Method to return the temperature in DegC. Resolution is 0.01 DegC. Output value of “5123” equals 51.23 DegC.
        private double BMP280_compensate_T_double(int adc_T)
        {
            double var1, var2, T;

            //The temperature is calculated using the compensation formula in the BMP280 datasheet
            var1 = ((adc_T / 16384.0) - (_calibrationData.DigT1 / 1024.0)) * _calibrationData.DigT2;
            var2 = ((adc_T / 131072.0) - (_calibrationData.DigT1 / 8192.0)) * _calibrationData.DigT3;

            t_fine = (int)(var1 + var2);

            T = (var1 + var2) / 5120.0;
            return T;
        }

        //Method to returns the pressure in Pa, in Q24.8 format (24 integer bits and 8 fractional bits).
        //Output value of “24674867” represents 24674867/256 = 96386.2 Pa = 963.862 hPa
        private long BMP280_compensate_P_Int64(int adc_P)
        {
            long var1, var2, p;

            //The pressure is calculated using the compensation formula in the BMP280 datasheet
            var1 = t_fine - 128000;
            var2 = var1 * var1 * (long)_calibrationData.DigP6;
            var2 = var2 + ((var1 * (long)_calibrationData.DigP5) << 17);
            var2 = var2 + ((long)_calibrationData.DigP4 << 35);
            var1 = ((var1 * var1 * (long)_calibrationData.DigP3) >> 8) + ((var1 * (long)_calibrationData.DigP2) << 12);
            var1 = (((((long)1 << 47) + var1)) * (long)_calibrationData.DigP1) >> 33;
            if (var1 == 0)
            {
                Debug.WriteLine("BMP280_compensate_P_Int64 Jump out to avoid / 0");
                return 0; //Avoid exception caused by division by zero
            }
            //Perform calibration operations as per datasheet: http://www.adafruit.com/datasheets/BST-BMP280-DS001-11.pdf
            p = 1048576 - adc_P;
            p = (((p << 31) - var2) * 3125) / var1;
            var1 = ((long)_calibrationData.DigP9 * (p >> 13) * (p >> 13)) >> 25;
            var2 = ((long)_calibrationData.DigP8 * p) >> 19;
            p = ((p + var1 + var2) >> 8) + ((long)_calibrationData.DigP7 << 4);
            return p;
        }


    }
}
