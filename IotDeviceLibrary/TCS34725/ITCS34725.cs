using System.Threading.Tasks;

namespace IotDeviceLibrary.TCS34725
{
    public interface ITCS34725 : IDevice
    {
        void SetGain(Gain gain);
        void SetIntegrationTime(IntegrationTime integrationTime);
        double CalculateColorTemperature(short r, short g, short b);
        double CalculateLux(short r, short g, short b);
    }

    public enum Gain : byte
    {
        TCS34725_GAIN_1X = 0x00,    /**  No gain  */
        TCS34725_GAIN_4X = 0x01,    /**  4x gain  */
        TCS34725_GAIN_16X = 0x02,   /**  16x gain */
        TCS34725_GAIN_60X = 0x03    /**  60x gain */
    }

    public enum IntegrationTime : byte
    {
        TCS34725_INTEGRATIONTIME_2_4MS = 0xFF,  /**  2.4ms - 1 cycle    - Max Count: 1024  */
        TCS34725_INTEGRATIONTIME_24MS = 0xF6,   /**  24ms  - 10 cycles  - Max Count: 10240 */
        TCS34725_INTEGRATIONTIME_50MS = 0xEB,   /**  50ms  - 20 cycles  - Max Count: 20480 */
        TCS34725_INTEGRATIONTIME_101MS = 0xD5,  /**  101ms - 42 cycles  - Max Count: 43008 */
        TCS34725_INTEGRATIONTIME_154MS = 0xC0,  /**  154ms - 64 cycles  - Max Count: 65535 */
        TCS34725_INTEGRATIONTIME_700MS = 0x00   /**  700ms - 256 cycles - Max Count: 65535 */
    }
}
