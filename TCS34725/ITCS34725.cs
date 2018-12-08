using System.Threading.Tasks;

namespace Iot.Device
{
    public interface ITCS34725
    {
        void SetGain(TCS34725_Gain gain);
        void SetIntegrationTime(TCS34725_IntegrationTime integrationTime);
        double CalculateColorTemperature(short r, short g, short b);
        double CalculateLux(short r, short g, short b);
        Task InitializeAsync();
        void Begin();
    }
}
