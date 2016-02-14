
namespace IotDeviceLibrary.TCS34725
{
    public class TCS34725Color
    {
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
        public byte Clear { get; set; }

        public TCS34725Color(byte red, byte green, byte blue, byte clear)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Clear = clear;
        }
    }
}
