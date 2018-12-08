using System;
using System.Threading.Tasks;

namespace Iot.Device
{
    public interface IDevice : IDisposable
    {
        Task InitializeAsync();
        void Begin();
    }
}
