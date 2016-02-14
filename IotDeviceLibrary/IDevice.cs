using System.Threading.Tasks;

namespace IotDeviceLibrary
{
    public interface IDevice
    {
        Task Initialize();
        Task Begin();
    }
}
