using Scb_Electronmash.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scb_Electronmash
{
    public interface IBluetooth_service
    {

        //  событие для передачи найденных устройств
        public event Action<Device_info> DeviceDiscovered;
        // Новое событие для уведомления об окончании поиска устройств
        event Action DiscoveryFinished;
        Task<bool> StartScanningAsync();
        Task<bool> OnOffBluetooth(bool turnOn);

        //  событие о смене состояния Bluetooth (true = включён)
        event Action<bool> BluetoothStateChanged;

        //  метод для опроса текущего состояния блютуз адаптера
        Task<bool> IsBluetoothEnabledAsync();

        // задача для соединения с устройством
        Task<bool> ConnectToDeviceAsync(Device_info deviceInfo);


    }
}
