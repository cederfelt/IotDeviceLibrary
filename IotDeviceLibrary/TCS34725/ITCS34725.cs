namespace IotDeviceLibrary.TCS34725
{
    public interface ITCS34725 : IDevice
    {
        void SetGain(TCS34725_Gain gain);
        void SetIntegrationTime(TCS34725_IntegrationTime integrationTime);
        double CalculateColorTemperature(short r, short g, short b);
        double CalculateLux(short r, short g, short b);
    }

    public enum TCS34725_Gain : byte
    {
        GAIN_1X = 0x00,    /**  No gain  */
        GAIN_4X = 0x01,    /**  4x gain  */
        GAIN_16X = 0x02,   /**  16x gain */
        GAIN_60X = 0x03    /**  60x gain */
    }

    public enum TCS34725_IntegrationTime : byte
    {
        T2_4MS = 0xFF,  /**  2.4ms - 1 cycle    - Max Count: 1024  */
        T24MS = 0xF6,   /**  24ms  - 10 cycles  - Max Count: 10240 */
        T50MS = 0xEB,   /**  50ms  - 20 cycles  - Max Count: 20480 */
        T101MS = 0xD5,  /**  101ms - 42 cycles  - Max Count: 43008 */
        T154MS = 0xC0,  /**  154ms - 64 cycles  - Max Count: 65535 */
        T700MS = 0x00   /**  700ms - 256 cycles - Max Count: 65535 */
    }
}
