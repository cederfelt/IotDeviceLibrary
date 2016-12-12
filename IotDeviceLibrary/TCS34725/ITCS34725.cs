namespace IotDeviceLibrary.TCS34725
{
    public interface ITCS34725 : IDevice
    {
        void SetGain(TCS34725_Gain gain);
        void SetIntegrationTime(TCS34725_IntegrationTime integrationTime);
        double CalculateColorTemperature(short r, short g, short b);
        double CalculateLux(short r, short g, short b);
    }
}
