using Scb_Electronmash.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// делегат для события очистки списка устройств
public delegate void Clear_EventHandler();



namespace Scb_Electronmash
{
    public interface IBluetooth_service
    {

        //  событие для передачи найденных устройств
        public event Action<Device_info> DeviceDiscovered;
        // Новое событие для уведомления об окончании поиска устройств
        event Action DiscoveryFinished;

        // Событие: вызывается при получении строки данных из Bluetooth (например, строки с датчика)
        public event Action<string> DataReceived;

        // событие для очистки списка устройств
        public event Clear_EventHandler Clear_Devices;

        //событие для передачи данных
        public event Action Data_To_Send;




        //задача для начала сканирования устройств
        Task<bool> StartScanningAsync();
        // задача для включения/выключения Bluetooth адаптера   
        Task<bool> OnOffBluetooth(bool turnOn);

        //  событие о смене состояния Bluetooth (true = включён)
        event Action<bool> BluetoothStateChanged;

        //  метод для опроса текущего состояния блютуз адаптера
        Task<bool> IsBluetoothEnabledAsync();

        // задача для соединения с устройством
        Task<bool> ConnectToDeviceAsync(Device_info deviceInfo);

        //задача по приему данных
        Task ReceiverData();

        // задача по очистке данных
        Task ClearData();

        // задача по передаче данных
        Task TransmitterData();

        // перегрузка метода по передаче данных с парамет
        Task TransmitterData(string asciiHex);

    }
}
