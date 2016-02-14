using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace IotDeviceLibrary
{
    public abstract class Device : IDevice
    {
        protected I2cDevice I2CDevice;

        public abstract Task Initialize();
        public abstract Task Begin();

        protected Boolean initialised = false;
        public Boolean Initilized { get { return initialised; } }

        //Method to read an 8-bit value from a register
        protected virtual byte ReadByte(byte register)
        {
            byte value = 0;
            byte[] writeBuffer = new byte[] { 0x00 };
            byte[] readBuffer = new byte[] { 0x00 };

            writeBuffer[0] = register;

            I2CDevice.WriteRead(writeBuffer, readBuffer);
            value = readBuffer[0];
            return value;
        }
    }
}
