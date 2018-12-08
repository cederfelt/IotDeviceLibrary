namespace Iot.Device
{
    public enum TCS34725_IntegrationTime : byte
    {
        /// <summary>
        /// 2.4ms - 1 cycle    - Max Count: 1024
        /// </summary>
        T2_4MS = 0xFF,
        /// <summary>
        /// 24ms  - 10 cycles  - Max Count: 10240
        /// </summary>
        T24MS = 0xF6,
        /// <summary>
        /// 50ms  - 20 cycles  - Max Count: 20480
        /// </summary>
        T50MS = 0xEB,
        /// <summary>
        /// 101ms - 42 cycles  - Max Count: 43008
        /// </summary>
        T101MS = 0xD5,
        /// <summary>
        /// 154ms - 64 cycles  - Max Count: 65535
        /// </summary>
        T154MS = 0xC0,
        /// <summary>
        /// 700ms - 256 cycles - Max Count: 65535
        /// </summary>
        T700MS = 0x00
    }
}
