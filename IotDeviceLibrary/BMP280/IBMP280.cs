using System.Threading.Tasks;

namespace IotDeviceLibrary.BMP280
{
    public interface IBMP280 : IDevice
    {
        float ReadTemperature();
        float ReadPreasure();
        float ReadAltitude(float seaLevel);
        
    }
}
