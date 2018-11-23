using System.Threading.Tasks;

namespace IotDeviceLibrary
{
    public interface IDevice
    {
        Task InitializeAsync();
        void Begin();
    }
}
