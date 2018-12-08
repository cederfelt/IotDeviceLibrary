namespace Iot.Device
{
    public interface IBME280
    {
        double ReadTemperature();
        double readPressure();
        double ReadHumidity();
        double ReadAltitude(double seaLevel);
    }
}
