using System.Threading.Tasks;

namespace Iot.Device
{
    public interface IBMP280 : IDevice
    {
        double ReadTemperature();
        double ReadPreasure();
        double ReadAltitude(float seaLevel);
    }
}
