namespace IotDeviceLibrary.BME280
{
    public interface IBME280 : IDevice
    {
        double ReadTemperature();
        double readPressure();
        double ReadHumidity();
        double ReadAltitude(double seaLevel);
    }
}
