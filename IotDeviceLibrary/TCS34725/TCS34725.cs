using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace IotDeviceLibrary.TCS34725
{
    //https://github.com/adafruit/Adafruit_TCS34725
    public class TCS34725 : Device, ITCS34725
    {
        private enum Cycle : byte
        {
            TCS34725_PERS_NONE = 0,         //(0b0000),  /* Every RGBC cycle generates an interrupt                                */
            TCS34725_PERS_1_CYCLE = 1,      //(0b0001),  /* 1 clean channel value outside threshold range generates an interrupt   */
            TCS34725_PERS_2_CYCLE = 2,      //(0b0010),  /* 2 clean channel values outside threshold range generates an interrupt  */
            TCS34725_PERS_3_CYCLE = 3,      //(0b0011),  /* 3 clean channel values outside threshold range generates an interrupt  */
            TCS34725_PERS_5_CYCLE = 4,      //(0b0100),  /* 5 clean channel values outside threshold range generates an interrupt  */
            TCS34725_PERS_10_CYCLE = 5,     //(0b0101),  /* 10 clean channel values outside threshold range generates an interrupt */
            TCS34725_PERS_15_CYCLE = 6,     //(0b0110),  /* 15 clean channel values outside threshold range generates an interrupt */
            TCS34725_PERS_20_CYCLE = 7,     //(0b0111),  /* 20 clean channel values outside threshold range generates an interrupt */
            TCS34725_PERS_25_CYCLE = 8,     //(0b1000),  /* 25 clean channel values outside threshold range generates an interrupt */
            TCS34725_PERS_30_CYCLE = 9,     //(0b1001),  /* 30 clean channel values outside threshold range generates an interrupt */
            TCS34725_PERS_35_CYCLE = 10,    //(0b1010),  /* 35 clean channel values outside threshold range generates an interrupt */
            TCS34725_PERS_40_CYCLE = 11,    //(0b1011),  /* 40 clean channel values outside threshold range generates an interrupt */
            TCS34725_PERS_45_CYCLE = 12,    //(0b1100),  /* 45 clean channel values outside threshold range generates an interrupt */
            TCS34725_PERS_50_CYCLE = 13,    //(0b1101),  /* 50 clean channel values outside threshold range generates an interrupt */
            TCS34725_PERS_55_CYCLE = 14,    //(0b1110),  /* 55 clean channel values outside threshold range generates an interrupt */
            TCS34725_PERS_60_CYCLE = 15     //(0b1111),  /* 60 clean channel values outside threshold range generates an interrupt */
        }

        private enum Registers : byte
        {
            ENABLE = 0x00,
            ENABLE_AIEN = 0x10,             // RGBC Interrupt Enable
            ENABLE_WEN = 0x08,              //Wait enable - Writing 1 activaes the wait timer
            TCS34725_ENABLE_AEN = (0x02),   /* RGBC Enable - Writing 1 actives the ADC, 0 disables it */
            TCS34725_ENABLE_PON = (0x01),   /* Power on - Writing 1 activates the internal oscillator, 0 disables it */
            TCS34725_ATIME = (0x01),        /* Integration time */
            TCS34725_WTIME = (0x03),        /* Wait time (if TCS34725_ENABLE_WEN is asserted) */
            TCS34725_WTIME_2_4MS = (0xFF),  /* WLONG0 = 2.4ms   WLONG1 = 0.029s */
            TCS34725_WTIME_204MS = (0xAB),  /* WLONG0 = 204ms   WLONG1 = 2.45s  */
            TCS34725_WTIME_614MS = (0x00),  /* WLONG0 = 614ms   WLONG1 = 7.4s   */
            TCS34725_AILTL = (0x04),        /* Clear channel lower interrupt threshold */
            TCS34725_AILTH = (0x05),
            TCS34725_AIHTL = (0x06),        /* Clear channel upper interrupt threshold */
            TCS34725_AIHTH = (0x07),
            TCS34725_PERS = (0x0C),         /* Persistence register - basic SW filtering mechanism for interrupts */
            TCS34725_CONFIG = (0x0D),
            TCS34725_CONFIG_WLONG = (0x02), /* Choose between short and long (12x) wait times via TCS34725_WTIME */
            TCS34725_CONTROL = (0x0F),      /* Set the gain level for the sensor */
            TCS34725_ID = (0x12),           /* 0x44 = TCS34721/TCS34725, 0x4D = TCS34723/TCS34727 */
            TCS34725_STATUS = (0x13),
            TCS34725_STATUS_AINT = (0x10),  /* RGBC Clean channel interrupt */
            TCS34725_STATUS_AVALID = (0x01),/* Indicates that the RGBC channels have completed an integration cycle */
            TCS34725_CDATAL = (0x14),       /* Clear channel data */
            TCS34725_CDATAH = (0x15),
            TCS34725_RDATAL = (0x16),       /* Red channel data */
            TCS34725_RDATAH = (0x17),
            TCS34725_GDATAL = (0x18),       /* Green channel data */
            TCS34725_GDATAH = (0x19),
            TCS34725_BDATAL = (0x1A),       /* Blue channel data */
            TCS34725_BDATAH = (0x1B),
        }

        private readonly byte CommandBit = 0x80;

        private const string I2CControllerName = "I2C1";

        private TCS34725_Gain _tcs34725Gain;
        private TCS34725_IntegrationTime _tcs34725IntegrationTime;

        public TCS34725(TCS34725_IntegrationTime time = TCS34725_IntegrationTime.T2_4MS, TCS34725_Gain gain = TCS34725_Gain.GAIN_1X, byte address = 0x29, byte commandbit = 0x80) : base(address, 0)
        {
            _tcs34725IntegrationTime = time;
            _tcs34725Gain = gain;
            CommandBit = commandbit;
        }

        public override async Task Initialize()
        {

            Debug.WriteLine("TCS34725 initialized");
            try
            {
                I2cConnectionSettings settings = new I2cConnectionSettings(Address);

                settings.BusSpeed = I2cBusSpeed.FastMode;

                String aqs = I2cDevice.GetDeviceSelector(I2CControllerName);

                DeviceInformationCollection dic = await DeviceInformation.FindAllAsync(aqs);

                I2CDevice = await I2cDevice.FromIdAsync(dic[0].Id, settings);

                if (I2CDevice == null)
                {
                    Debug.WriteLine("Device not found");
                }
                initialised = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message + "\n" + e.StackTrace);
                throw;
            }
        }

        /**************************************************************************/
        /*! 
            Initializes I2C and configures the sensor (call this function before 
            doing anything else) 
        */
        /**************************************************************************/
        public override void Begin()
        {
            Debug.WriteLine("TCS34725 BEGIN");

            /* Make sure we're actually connected */
            byte x = Read8((byte)Registers.TCS34725_ID);
            if ((x != 0x44) && (x != 0x10))
            {
                return;
            }
            initialised = true;

            /* Note: by default, the device is in power down mode on bootup */
            Enable();

            //return true;
        }

        public void SetGain(TCS34725_Gain gain)
        {
            _tcs34725Gain = gain;
        }

        public void SetIntegrationTime(TCS34725_IntegrationTime integrationTime)
        {
            _tcs34725IntegrationTime = integrationTime;
        }

        private short ReadShort(byte reg)
        {
            byte[] writeBuffer = new byte[] { 0x00 };
            byte[] readBuffer = new byte[] { 0x00 };
            writeBuffer[0] = reg;
            I2CDevice.WriteRead(writeBuffer, readBuffer);
            var value = readBuffer[0];
            return value;
        }

        public void Write(byte register, byte data)
        {
            byte[] writeBuffer = new byte[] { register, data };
            I2CDevice.Write(writeBuffer);
        }

        /**************************************************************************/
        /*! 
            @brief  Reads the raw red, green, blue and clear channel values 
        */
        /**************************************************************************/
        private async Task<TCS34725Color> GetRawData()
        {
            if (!initialised) Begin();

            byte c = Read8((byte)Registers.TCS34725_CDATAL);
            byte r = Read8((byte)Registers.TCS34725_RDATAL);
            byte g = Read8((byte)Registers.TCS34725_GDATAL);
            byte b = Read8((byte)Registers.TCS34725_BDATAL);

            /* Set a delay for the integration time */
            switch (_tcs34725IntegrationTime)
            {
                case TCS34725_IntegrationTime.T2_4MS:
                    //delay(3);
                    await Task.Delay(3);
                    break;
                case TCS34725_IntegrationTime.T24MS:
                    //delay(24);
                    await Task.Delay(24);
                    break;
                case TCS34725_IntegrationTime.T50MS:
                    //delay(50);
                    await Task.Delay(50);
                    break;
                case TCS34725_IntegrationTime.T101MS:
                    //delay(101);
                    await Task.Delay(101);
                    break;
                case TCS34725_IntegrationTime.T154MS:
                    //delay(154);
                    await Task.Delay(154);
                    break;
                case TCS34725_IntegrationTime.T700MS:
                    //delay(700);
                    await Task.Delay(700);
                    break;
            }
            return new TCS34725Color(r, g, b, c);
        }

        /**************************************************************************/
        /*! 
            Enables the device 
        */
        /**************************************************************************/
        private async void Enable()
        {
            Write((byte)Registers.ENABLE, (byte)Registers.TCS34725_ENABLE_PON);
            await Task.Delay(3);
            Write((byte)Registers.ENABLE, (byte)Registers.TCS34725_ENABLE_PON | (byte)Registers.TCS34725_ENABLE_AEN);
        }

        /**************************************************************************/
        /*! 
            Disables the device (putting it in lower power sleep mode) 
        */
        /**************************************************************************/
        private void Disable()
        {
            /* Turn the device off to save power */
            byte reg = 0;
            reg = Read8((byte)Registers.ENABLE);
            int value = ~(((byte)Registers.TCS34725_ENABLE_PON | (byte)Registers.TCS34725_ENABLE_AEN));
            Write((byte)Registers.ENABLE, (byte)(reg & value));
        }

        /**************************************************************************/
        /*! 
            @brief  Converts the raw R/G/B values to color temperature in degrees 
                    Kelvin 
        */
        /**************************************************************************/
        public double CalculateColorTemperature(short r, short g, short b)
        {
            double X, Y, Z;      /* RGB to XYZ correlation      */
            double xc, yc;       /* Chromaticity co-ordinates   */
            double n;            /* McCamy's formula            */
            double cct;

            /* 1. Map RGB values to their XYZ counterparts.    */
            /* Based on 6500K fluorescent, 3000K fluorescent   */
            /* and 60W incandescent values for a wide range.   */
            /* Note: Y = Illuminance or lux                    */
            X = (-0.14282F * r) + (1.54924F * g) + (-0.95641F * b);
            Y = (-0.32466F * r) + (1.57837F * g) + (-0.73191F * b);
            Z = (-0.68202F * r) + (0.77073F * g) + (0.56332F * b);

            /* 2. Calculate the chromaticity co-ordinates      */
            xc = (X) / (X + Y + Z);
            yc = (Y) / (X + Y + Z);

            /* 3. Use McCamy's formula to determine the CCT    */
            n = (xc - 0.3320F) / (0.1858F - yc);

            /* Calculate the final CCT */
            cct = (449.0F * Math.Pow(n, 3)) + (3525.0F * Math.Pow(n, 2)) + (6823.3F * n) + 5520.33F;

            /* Return the results in degrees Kelvin */
            return cct;
        }

        /**************************************************************************/
        /*! 
            @brief  Converts the raw R/G/B values to lux 
        */
        /**************************************************************************/
        public double CalculateLux(short r, short g, short b)
        {
            float illuminance;

            /* This only uses RGB ... how can we integrate clear or calculate lux */
            /* based exclusively on clear since this might be more reliable?      */
            illuminance = (-0.32466f * r) + (1.57837f * g) + (-0.73191f * b);

            return illuminance;
        }
    }
}
