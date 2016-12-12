using System.Threading.Tasks;

namespace IotDeviceLibrary.BMP280
{
    public interface IBMP280 : IDevice
    {
        double ReadTemperature();
        double ReadPreasure();
        double ReadAltitude(float seaLevel);
    }
}
