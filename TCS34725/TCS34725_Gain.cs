namespace Iot.Device
{
    public enum TCS34725_Gain : byte
    {
        /// <summary>
        /// No Gain
        /// </summary>
        GAIN_1X = 0x00,
        /// <summary>
        /// 4x Gain
        /// </summary>
        GAIN_4X = 0x01,
        /// <summary>
        /// 16x Gain
        /// </summary>
        GAIN_16X = 0x02,
        /// <summary>
        /// 60x Gain
        /// </summary>
        GAIN_60X = 0x03
    }
}
