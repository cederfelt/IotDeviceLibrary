using System.Threading.Tasks;

namespace IotDeviceLibrary.BMP280
{
    public interface IBMP280 : IDevice
    {
        Task<float> ReadTemperature();
        Task<float> ReadPreasure();
        Task<float> ReadAltitude();
        Task<BMP280_CalibrationData> ReadCoefficeints();
    }
}
