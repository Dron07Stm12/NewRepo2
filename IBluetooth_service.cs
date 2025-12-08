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


        //Task TransmitterData(string dataAscii,
        //                          byte start,         // логическое поле start внутри кадра (обычно 0x01 или 0x02)
        //                          byte read,          // поле read (в примерах 0x01)
        //                          byte[] index,       // 4 байта index, по умолчанию 00 00 22 00
        //                          byte subindex,      // subindex, по умолчанию 0x01
        //                          bool wrapWithRawStartStop = true // оборачивать ли кадр rawStart/rawStop при отправке
        //                      );


        Task TransmitterData_write(string dataAscii, byte read = 0x02, byte address = 0x01, byte[] index = null, byte? subindex = null, bool wrapWithRawStartStop = true);



    }
}
