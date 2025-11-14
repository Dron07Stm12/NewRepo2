using Android;
using Android.App;
using Android.Bluetooth;
using Android.Content;  
using Android.Content.PM;
using Android.Health.Connect.DataTypes.Units;
using Android.OS;
using Android.Provider; 
using Android.Util;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.Maui.Controls;
using Org.W3c.Dom;
using Scb_Electronmash.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.Provider.Contacts.Intents;
using static Microsoft.Maui.ApplicationModel.Permissions;
using static System.Net.Mime.MediaTypeNames;
//
//using MauiApp = Microsoft.Maui.Controls.Application;
//using Application = Android.App.Application;

namespace Scb_Electronmash.Platforms.Android
{
    public class AndroidBluetooth : IBluetooth_service
    {

        private BluetoothAdapter? _adapter;
        private Context? _context;
        private BroadcastReceiver _receiver;
        private BroadcastReceiver? _stateReceiver;
        public  BluetoothSocket? bluetoothSocket;

        private CancellationTokenSource? _readCts;

        // поле в классе сервиса
        private volatile bool _rxRunning = false;




#pragma warning disable CS8618 // Поле, не допускающее значения NULL, должно содержать значение, отличное от NULL, при выходе из конструктора. Рассмотрите возможность добавления модификатора "required" или объявления значения, допускающего значение NULL.
        public AndroidBluetooth()
#pragma warning restore CS8618 // Поле, не допускающее значения NULL, должно содержать значение, отличное от NULL, при выходе из конструктора. Рассмотрите возможность добавления модификатора "required" или объявления значения, допускающего значение NULL.
        {
#pragma warning disable CA1422 // Проверка совместимости платформы
            _adapter = BluetoothAdapter.DefaultAdapter;
#pragma warning restore CA1422 // Проверка совместимости платформы
            _context = Platform.AppContext;//это глобальный контекст приложения, не привязанный к окну/экрану (Activity).


            // формируем intent filter и регистрируем ресивер для отслеживания изменений состояния Bluetooth
            try
            {
                if (_context != null)
                {
                    _stateReceiver = new BroadcastReceiverStateChanged(this);
                    var stateFilter = new IntentFilter(BluetoothAdapter.ActionStateChanged);
                    _context.RegisterReceiver(_stateReceiver, stateFilter);
                }
            }
            catch
            {
                // безопасно игнорируем ошибки регистрации (например, если контекст ещё не готов)
            }




        }

        //        Что делает StartScanningAsync

        //Проверяет наличие адаптера и что он включён.
        //Берёт активность через Platform.CurrentActivity as MainActivity (если activity == null — возвращает false).
        //Запрашивает(асинхронно) runtime‑разрешения через BluetoothPermissionsHelper.
        //Если был ранее зарегистрированный _receiver — отписывается от него и очищает поле.
        //Создаёт новый экземпляр Device_Receiver, передавая в конструктор два делегата:
        //делегат для каждого найденного устройства — внутри него ты вызываешь событие DeviceDiscovered?.Invoke(onDeviceFound);
        //        делегат для окончания discovery — внутри него вызываешь DiscoveryFinished?.Invoke();
        //        Формирует IntentFilter(ActionFound + ActionDiscoveryFinished).
        //Регистрирует ресивер в _context: _context.RegisterReceiver(_receiver, filter);
        //Запускает _adapter.StartDiscovery() — асинхронный inquiry, результаты придут в BroadcastReceiver.


//        Что такое _receiver с делегатами

//_receiver — экземпляр твоего класса Device_Receiver.В его конструкторе ты передаёшь два делегата(Action<Device_info> и Action).
//Device_Receiver реализует BroadcastReceiver.OnReceive и при получении интента парсит данные(например, BluetoothDevice при ActionFound), создаёт Device_info и вызывает переданный делегат: _onDeviceFound?.Invoke(device_info).
//Это фактически «указатели на методы» — ссылки на функции, которые будут вызваны при поступлении соответствующего Broadcast.
        public event Action<Device_info> DeviceDiscovered;  
        public event Action DiscoveryFinished;
        public event Action<bool> BluetoothStateChanged;
        public async Task<bool> StartScanningAsync()
        {
            if (_adapter == null || !_adapter.IsEnabled)
                return false;

            // Platform.CurrentActivity возвращает тип Activity,
            //а  код ожидает именно мой класс активности — MainActivity.
           // Оператор as пытается привести объект к типу MainActivity.
           //Если привести не удалось(например, если активити — это не твой класс, а другой) — результат будет null, а не Exception.
            var activity = Platform.CurrentActivity as MainActivity;
            if (activity == null)
                return false;
            // Запрашиваем разрешения у пользователя - если их ещё нет
            bool granted = await BluetoothPermissionsHelper.RequestBluetoothPermissionsAsync(activity);
            if (!granted)
                return false;
            //Очистка старого Receiver (если был)
            if (_receiver != null)
            {
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
#pragma warning disable CA1416 // Проверка совместимости платформы
                _context.UnregisterReceiver(_receiver);
#pragma warning restore CA1416 // Проверка совместимости платформы
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
                _receiver = null;
            }

            // Создаём новый ресивер и передаём ему два делегата:
            //  - при обнаружении устройства вызывается DeviceDiscovered (если кто-то на него подписан)
            //  - при завершении обнаружения вызывается DiscoveryFinished
            // Итого: да — эти анонимные делегаты — и есть те «методы», которые будут вызываться внутри _receiver
            // и которые в свою очередь триггерят события.
            _receiver = new Device_Receiver(

                delegate (Device_info onDeviceFound) { DeviceDiscovered?.Invoke(onDeviceFound);},
                delegate () { DiscoveryFinished?.Invoke();}
              
            );
            // Формируем фильтр интентов: интересуют события найденного устройства и завершения поиска.
            IntentFilter filter = new IntentFilter(BluetoothDevice.ActionFound);//// уведомления о найденных устройствах
            filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished); // Добавляем фильтр для события уведомление об окончании discovery
          
            // Регистрируем ресивер в контексте приложения. После регистрации он будет получать указанные Broadcast'ы.
            _context.RegisterReceiver(_receiver, filter);
            //// Запускаем процесс классического Bluetooth-сканирования (inquiry). Это асинхронный процесс, результаты придут через BroadcastReceiver.
            _adapter.StartDiscovery();


            // Toast.MakeText(_context, "Сканирование началось", ToastLength.Short).Show();
            // Возвращаем true — сканирование успешно инициировано (фактические устройства будут приходить в событии DeviceDiscovered).
            return true;
        }


        public Task<bool> IsBluetoothEnabledAsync()
        {
            bool enabled = _adapter != null && _adapter.IsEnabled;
            return Task.FromResult(enabled);
        }






        public async Task<bool> OnOffBluetooth(bool turnOn)
        {
            // Проверяем, существует ли Bluetooth-адаптер на устройстве.
            if (_adapter == null) return false; // Нет Bluetooth адаптера

            // Определяем, современный ли Android (13+).
            bool isModernAndroid = Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu; // Android 13+

            // Если нужно включить Bluetooth.
            if (turnOn)
            {
                // Если Bluetooth уже включён, ничего делать не надо.
                if (_adapter.IsEnabled)
                    return true; // Уже включён, возвращаем успех

                // Для Android 13+ нельзя включить программно — открываем настройки Bluetooth.
                if (isModernAndroid)
                {
                    var intent = new Intent(Settings.ActionBluetoothSettings); // Формируем интент для открытия настроек Bluetooth
                    intent.AddFlags(ActivityFlags.NewTask); // Открыть настройки в новом task (важно для контекста не-Activity)
                    _context?.StartActivity(intent); // Запускаем настройки Bluetooth
                    return false; // Пользователь должен включить Bluetooth вручную
                }
                else
                {
                    // Программно включаем Bluetooth (Android < 13)
                    bool enabled = _adapter.Enable(); // Отправляем команду на включение Bluetooth
                    int tries = 10; // Счётчик попыток ожидания включения
                    while (!_adapter.IsEnabled && tries-- > 0)
                        await Task.Delay(300); // Ждём, пока Bluetooth включится (не сразу)
                    return _adapter.IsEnabled; // Возвращаем, удалось ли включить
                }
            }
            // Если нужно выключить Bluetooth.
            else // turnOn == false, выключить Bluetooth
            {
                // Если Bluetooth уже выключен, ничего делать не надо.
                if (!_adapter.IsEnabled)
                    return true; // Уже выключен, возвращаем успех

                // Для Android 13+ нельзя выключить программно — открываем настройки Bluetooth.
                if (isModernAndroid)
                {
                    var intent = new Intent(Settings.ActionBluetoothSettings); // Формируем интент для открытия настроек Bluetooth
                    intent.AddFlags(ActivityFlags.NewTask); // Открыть настройки в новом task
                    _context?.StartActivity(intent); // Запускаем настройки Bluetooth
                    return false; // Пользователь должен выключить Bluetooth вручную
                }
                else
                {
                    // Программно выключаем Bluetooth (Android < 13)
                    bool disabled = _adapter.Disable(); // Отправляем команду на выключение Bluetooth
                    int tries = 10; // Счётчик попыток ожидания выключения
                    while (_adapter.IsEnabled && tries-- > 0)
                        await Task.Delay(300); // Ждём, пока Bluetooth выключится (не сразу)
                    return !_adapter.IsEnabled; // Возвращаем, удалось ли выключить
                }
            }
        }

        // CHANGED: вызываем метод владельца, чтобы событие вызывалось из типа AndroidBluetooth
        public void RaiseBluetoothStateChanged(bool enabled)
        {
            BluetoothStateChanged?.Invoke(enabled);
        }


        // заменяем текущий ConnectToDeviceAsync на этот вариант
        public async Task<bool> ConnectToDeviceAsync(Device_info deviceInfo)
        {

            // Получаем удалённое устройство по его MAC-адресу.
            // используется условный оператор ?. — если _adapter == null, device будет null. 
            // Что делает: у адаптера Bluetooth (поле _adapter) вызывается GetRemoteDevice с MAC-адресом.
            // Возвращает объект BluetoothDevice, представляющий удалённое устройство.
            // нужен объект BluetoothDevice для создания сокета и подключения
            var device = _adapter?.GetRemoteDevice(deviceInfo.Address);
            // Устройство не найдено
            if (device == null)
            {
                Log.Error("BTPerms", $"GetRemoteDevice returned null for {deviceInfo?.Address}");
                return false;
            }

            // //try/catch: если отмена discovery вызовет исключение, оно будет поймано и залогировано как предупреждение.
            try
            {
                // отменяет текущее Bluetooth discovery (сканирование).
                _adapter?.CancelDiscovery();
                // Логирование: Info о том, что отмена вызвана.           
                Log.Info("BTPerms", "CancelDiscovery called before connect");
            }
            catch (Exception ex)
            {
                Log.Warn("BTPerms", $"CancelDiscovery failed: {ex}");
            }
            // Стандартный UUID для Serial Port Profile (SPP)   
            Java.Util.UUID sppUuid = Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");
            // Локальная переменная для сокета
            // создание сокета локально, а не сразу присваивание глобальному полю — удобнее при ошибках и закрытии.
            BluetoothSocket localSocket = null;

            try
            {
                // Создаём небезопасный RFCOMM сокет для подключения к устройству по SPP UUID
                // Небезопасный означает, что не используется шифрование или аутентификация
                // CreateInsecureRfcommSocketToServiceRecord пропускает часть безопасной шифровки/аутентификации,
                // часто проще для старых модулей (HC-06). CreateRfcommSocketToServiceRecord (secure) требует pairing/secure channel.
                localSocket = device.CreateInsecureRfcommSocketToServiceRecord(sppUuid);
                

                // Проверка на null (хотя CreateInsecure... обычно не возвращает null)
                if (localSocket == null)
                {
                    Log.Error("BTPerms", "localSocket is null");
                    return false;
                }

                // создание задачи для подключения сокета (ложим ее в пул потоков) и получаем маркер task
                // создаёт задачу, выполняющую вызов Connect() в пуле потоков, и возвращает объект Task, который служит «маркером» (handle) для этой фоновой работы
                Task task = Task.Run(delegate () { localSocket.Connect(); });

                //  var connectTask = Task.Run(() => localSocket.Connect());
                // Ждём либо завершения подключения, либо таймаута в 14 секунд  
                var completed = await Task.WhenAny(task, Task.Delay(14000)); // 14s timeout
              //Если первой завершилась Task.Delay (т.е. connect не успел за 14s), логируем timeout, закрываем socket и возвращаем false.
              //закрывать локальный сокет после таймаута важно, иначе ресурс остаётся открытым и мешает следующим попыткам.
                if (completed != task)
                {
                    Log.Error("BTPerms", "Connect timeout");
                    try { localSocket.Close(); } catch { }
                    return false;
                }

                ///////////////
                // Здесь completed == task — проверим его состояние
                if (task.IsCompletedSuccessfully)
                {
                    // Успешно — можно продолжать
                    bluetoothSocket = localSocket;
                }
                else if (task.IsCanceled)
                {
                    // Отменено
                    Log.Warn("BTPerms", "Connect was canceled");
                    try { localSocket.Close(); } catch { }
                    return false;
                }
                else if (task.IsFaulted)
                {
                    // Упало с исключением — Exception хранится в connectTask.Exception (AggregateException)
                    var agg = task.Exception; // AggregateException
                    var ex = agg?.GetBaseException(); // реальная причина
                    Log.Error("BTPerms", $"Connect failed: {ex}");
                    try { localSocket.Close(); } catch { }
                    return false;
                }

                

                Log.Info("BTPerms", $"Socket connected to {device.Address} name={device.Name}");
                return true;
            }
            catch (Java.IO.IOException ioEx)
            {
                Log.Error("BTPerms", $"IOException during connect/read: {ioEx}");
                try { localSocket?.Close(); } catch { }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("BTPerms", $"Exception during connect: {ex}");
                try { localSocket?.Close(); } catch { }
                return false;
            }
        }



        ////////////////////////
        public event Action<string> DataReceived; // 👉 событие для передачи данных в UI
                                                  //        public async Task ReceiverData()
                                                  //        {

        ////            //включение Foreground Service
        ////            _context = Platform.AppContext;
        ////            var intent = new Intent(_context, typeof(Bluetooth_Foregraund_service));
        ////            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        ////#pragma warning disable CA1416 // Проверка совместимости платформы
        ////                _context.StartForegroundService(intent); // Android 8+
        ////#pragma warning restore CA1416 // Проверка совместимости платформы
        ////            else
        ////                _context.StartService(intent); // Android < 8                  

        //            // Буфер для приёма "сырых" байтов из Bluetooth (4096 байт за раз).
        //            byte[] buffer = new byte[4096];
        //            // StringBuilder — для накопления текста, если сообщение приходит не целиком, а частями.
        //            StringBuilder dataBuffer = new StringBuilder();

        //            try
        //            {

        //                // Берём поток, с которого будем читать. Должен быть уже открыт и готов к чтению.
        //                var _inputStream = bluetoothSocket?.InputStream;


        //                if (_inputStream == null)
        //                {
        //                    // DataReceived?.Invoke("Ошибка: Bluetooth поток не инициализирован.");
        //                    // _myEvent?.Invoke("Вызов делегата: public delegate void MyEventHandler(string message);");
        //                    // MyEvent.Invoke("не явно реализованное событие");
        //                    //   return;
        //                }

        //                // Бесконечный цикл для непрерывного чтения данных, пока соединение активно.
        //                while (true)
        //                {

        //                    if (!_adapter.IsEnabled)
        //                    {
        //                        MainThread.BeginInvokeOnMainThread(() => {
        //                            Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Bluetooth", "Bluetooth is disabled", "OK");
        //                        });
        //                        bluetoothSocket?.Close(); // Закрываем сокет, если адаптер не инициализирован
        //                        _context.UnregisterReceiver(_receiver);
        //                        return;
        //                    }


        //                    // Делаем небольшую паузу, чтобы не грузить процессор.
        //                    await Task.Delay(100);
        //                    // Проверяем, можно ли читать из потока (соединение не закрыто и поток поддерживает чтение)
        //                    if (_inputStream.CanRead)
        //                    {
        //                        // Читаем данные из потока в буфер.
        //                        int bytesRead = await _inputStream.ReadAsync(buffer, 0, buffer.Length);
        //                        // Если что-то действительно прочитали...
        //                        if (bytesRead > 0)
        //                        {
        //                            // Преобразуем байты в строку (ASCII).
        //                            string part = Encoding.ASCII.GetString(buffer, 0, bytesRead);

        //                            // Добавляем прочитанную часть к накопленному тексту.
        //                            dataBuffer.Append(part);
        //                            // Если в пришедшей части есть символ новой строки, значит сообщение завершено.
        //                            if (part.Contains("\n"))
        //                            {

        //                                // Собираем полное сообщение, убираем лишние пробелы.
        //                                string completeMessage = dataBuffer.ToString().Trim();
        //                                // 👉 Передаем полученную строку через событие DataReceived.
        //                                // Если в MainPage подписка на это событие — она получит сообщение и обновит label4.
        //                                DataReceived?.Invoke(completeMessage);
        //                                //   if (DataReceived != null) { DataReceived(completeMessage); }
        //                                // Очищаем буфер, чтобы начать накопление следующего сообщения.
        //                                dataBuffer.Clear();
        //                            }

        //                        }


        //                    }



        //                }

        //            }
        //            catch (Exception ex)
        //            {

        //                // Если возникла ошибка (например, разрыв соединения),
        //                // отправляем сообщение об ошибке через то же событие в UI.
        //                DataReceived?.Invoke($"Error: {ex.Message}");
        //            }



        //        }

        /// ////////////////////////////////////
        // задача для приёма данных
        public async Task ReceiverData()
        {
            // если уже запущен приёмник, не запускаем ещё раз
            if (_rxRunning) return; // уже запущен
            _rxRunning = true;


            //Что делает: создаёт массив байт длиной 4096, который используется как временный буфер для чтения данных из InputStream (в одну операцию чтения).
           // Зачем нужен: Read / ReadAsync заполняет переданный массив считанными байтами;
           // размер буфера задаёт максимум байт, который можно получить за один вызов read. 4096 — типичный компромисс:
           // достаточно большой, чтобы вместить большие фрагменты, но не слишком большой, чтобы жрать много памяти.
            byte[] buffer = new byte[4096];

            // получаем поток для чтения данных из сокета
            // bluetoothSocket должен быть уже подключён и готов к чтению
            //Что делает: получает поток ввода (InputStream) из объекта bluetoothSocket 
            var input = bluetoothSocket?.InputStream;
            // проверяем, что поток не null
            if (input == null)
            {
            // если поток не инициализирован, вызываем событие с ошибкой и выходим
            // Что делает: вызывает событие DataReceived (если на него кто‑то подписан) с текстовым сообщением об ошибке.
            //Зачем: чтобы UI или другой код, подписанный на событие, узнал о проблеме.
            //В твоём MainPage подписка есть — OnDataReceived выполнит MainThread.BeginInvokeOnMainThread и покажет сообщение в label4.
                DataReceived?.Invoke("Error: InputStream is null");
                _rxRunning = false;
                return;
            }
            // список для накопления полученных байт
            //  List удобно использовать, потому что можно добавлять данные, удалять уже обработанные префиксы и искать в нём маркеры начала/конца.
            var recvBuf = new List<byte>();

            // Строки: const byte RESP_START = 0x02; const byte FRAME_STOP = 0x05;
            //Объяснение:
            //Что делает: объявляет две константы байтового типа,
            //задающие специальный протокольный маркер начала кадра(RESP_START = 0x02) и маркер конца кадра(FRAME_STOP = 0x05).
            const byte RESP_START = 0x02;
            const byte FRAME_STOP = 0x05;

            try
            {
                // внешний цикл: пока _rxRunning == true
                //Что делает: запускает основной цикл приёма данных, который повторяется,
                //пока флаг _rxRunning остаётся true. Это главная петля метода,
                //в которой происходит все проверки состояния, чтение из потока и разбор полученных байтов.
                // Зачем нужен: позволяет непрерывно слушать входящий поток без повторного вызова метода извне.
                // Управление циклом выполняется через флаг _rxRunning — внешние вызовы (в т.ч. StopReceiver) могут установить его в false,
                // и цикл корректно завершится.
                //Поведение с async / await: так как метод асинхронный и внутри цикла есть await (await Task.Delay / await Task.Run(...)),
                //цикл не будет блокировать поток вызова(UI). await уступает управление, позволяя другому коду исполняться,
                //а затем цикл продолжит работу при возобновлении.
                while (_rxRunning)
                {
                    // проверяем, что Bluetooth всё ещё включён
                    // Что делает: уведомляет подписчиков(UI) о том, что Bluetooth отключён и приёмник останавливается.
                    //Почему важно: даёт пользователю обратную связь(например, в label4 появится сообщение).
                    //Вызов остаётся безопасным, потому что обработчик в UI делает маршалинг в главный поток.
                    
                    if (!_adapter.IsEnabled)
                    {
                        DataReceived?.Invoke("Bluetooth disabled, stopping receiver");
                        // закрываем сокет, если адаптер не инициализирован
                        try { bluetoothSocket?.Close(); } catch { }
                        //Что делает: пытается отписать/удалить ранее зарегистрированный BroadcastReceiver из контекста приложения.
                        try { _context.UnregisterReceiver(_receiver); } catch { }
                        // break;
                        //Что делает: прекращает выполнение внешнего while (_rxRunning) и выходит к блоку finally (или к последующим инструкциям после цикла).
                        //Почему break, а не return: break даёт возможность корректно дойти до блока finally метода, где _rxRunning будет сброшен в false;
                        //также позволяет общий cleanup/ логирование в одном месте. return бы тоже работал,
                        //но тогда finally всё равно выполнится — в данном случае break просто логически завершает цикл, не прерывая выполнение метода внезапно.
                        break;
                    }
                    // приостанавливает выполнение текущей итерации асинхронного метода на ~30 миллисекунд без блокировки потока UI. 
                    await Task.Delay(30);

                    
                    // Объявление целочисленной переменной, которая будет хранить фактическое число байт,
                    // прочитанных одним вызовом Read. Используется далее, чтобы знать, сколько байт взять из буфера.
                    int bytesRead;
                    try
                    {
                        //Task.Run — метод, который запускает указанную работу (делегат) в пуле потоков (ThreadPool), т.е. в другом потоке, не в UI‑потоке.
                        // Аргумент Task.Run здесь — лямбда () => input.Read(buffer, 0, buffer.Length).
                        // Это функция без параметров, которая возвращает int (количество прочитанных байт).
                        //Тип делегата, который передаётся Task.Run в этом случае — Func<int>(функция, возвращающая int).

                        //Что возвращает Task.Run(...)
                        // Task.Run(() => input.Read(...)) возвращает объект типа Task<int>.
                        // Task<int> — это задание, которое при завершении содержит результат типа int.
                        //Когда ты await -ишь этот Task<int>, результатом await будет само int — то есть bytesRead получит значение, которое вернул input.Read.

                        //input.Read(...) возвращает int.
                        //Task.Run(() => input.Read(...))->Task<int>
                        //await Task.Run(...)-> int

                        //Если вызвать её прямо в UI‑потоке, UI замрёт. Поэтому мы запускаем её в ThreadPool
                        //через Task.Run — блокировка произойдёт в другом потоке, а UI остаётся отзывчивым.

                        //await ожидает завершения Task, но не блокирует текущий поток. Оно «отдаёт управление»
                        //(возвращает Task контролирующему коду) и продолжит метод позже, когда фоновая задача завершится.
                        //Во время ожидания другие операции(включая UI) могут выполняться.

                        //Что происходит «под капотом» пошагово
                        // Вызывается Task.Run с делегатом.Платформа помещает задачу в очередь ThreadPool.
                        //В какой‑то фоновый поток забирается этот делегат и выполняется input.Read(buffer, 0, buffer.Length).
                        //Этот Read блокирует тот фоновой поток, пока не придут данные(или сокет не закроют / не случится исключение).
                        //Когда Read возвращает число байт, Task<int> завершает работу и хранит результат.
                        //await наблюдает за Task; когда он завершился, await возвращает int и выполнение метода продолжаетсья(в твоём методе — дальше идёт обработка данных).

                        //ThreadPool — это набор рабочих потоков, которые .NET поддерживает и переиспользует для выполнения мелких задач.
                        //Когда вы делаете Task.Run(...) или ThreadPool.QueueUserWorkItem(...), задача помещается в очередь,
                        //и один из потоков пула выполняет её. Потоки не создаются/удаляются для каждой задачи — они переиспользуются, что экономит время и ресурсы.

                        bytesRead = await Task.Run(() => input.Read(buffer, 0, buffer.Length));
                    }
                    catch (Exception readEx)
                    {
                        //это вызов события (вызов всех подписанных обработчиков), и если есть подписчики,
                        //то они получат строку($"Receiver read error: {readEx.Message}") как аргумент (потому что у тебя event объявлен как Action<string>). 
                        //Что делает DataReceived?.Invoke(...)
                        //DataReceived — событие типа Action<string>.Вызов DataReceived?.Invoke(msg) эквивалентен:
                        //если DataReceived != null, вызвать каждый подписанный делегат с параметром msg.

                        //Делегат — это тип (и экземпляры этого типа)
                        //Делегат(например, Action<string> или delegate void MyHandler(string s)) — это ссылочный тип, представляющий указатель на метод(ы).
                        //Экземпляр делегата содержит ссылку на целевой объект(для instance‑метода) и на метод — или на список таких пар(multicast).
                        //Множественные обработчики и multicast‑делегат
                        //Делегат в.NET может быть «многоканальным» (multicast): он хранит список методов(invocation list).
                        //Когда вы вызываете такой делегат, он последовательно вызывает все методы из списка.
                        //Операции += и -= над делегатом фактически делают Combine/ Remove: += добавляет метод в конец списка, -= удаляет первое вхождение указанного метода.
                        //Событие(event) — это оболочка вокруг делегата
                        //event — это член класса, у которого тип — делегат (в твоём случае Action<string>). Пример: public event Action<string> DataReceived;
                        // Главное отличие: event даёт контроль доступа — извне класса ты можешь только подписаться(+=) или отписаться(-=).
                        // Вызвать(invoke) (т.е.вызвать делегат напрямую) можно только из того класса,
                        // где событие объявлено(или из типов, имеющих доступ к закрытому полю-делегату, если использовать явный backing field).
                        //Поэтому: делегат — тип; событие — поле/свойство чьего‑то класса, которое хранит делегат(ы) и ограничивает, кто может вызывать их.

                         DataReceived?.Invoke($"Receiver read error: {readEx.Message}");
                        break;
                    }

                    // if (bytesRead <= 0) continue; // continue возвращает управление в начало внешнего while (_rxRunning)
                    // если будет  bytesRead - нуль, то вернеться сюда:  while (_rxRunning)
                    if (bytesRead <= 0)
                    {
                        if (bytesRead == -1)
                        {
                            DataReceived?.Invoke("Remote closed (EOF)");
                            try { bluetoothSocket?.Close(); } catch { }
                            break;
                        }
                        continue;
                    }


                    //Цикл проходит от 0 до bytesRead-1 и поэлементно добавляет каждый прочитанный байт в конец списка recvBuf (List<byte>).
                    for (int i = 0; i < bytesRead; i++) recvBuf.Add(buffer[i]);

                    while (true)
                    {
                        //ищет в списке recvBuf индекс первого вхождения байта RESP_START (в твоём коде 0x02)
                        int start = recvBuf.IndexOf(RESP_START);
                        //Если в накопителе recvBuf не найден маркер START (start == -1), то:
                        //если recvBuf.Count > 8192 — очищает весь буфер(recvBuf.Clear()) чтобы не допустить бесконтрольного роста памяти;
                        //в любом случае выполняет break — выходит из внутреннего парсера(while (true)) и возвращается в внешний цикл, ожидая новых данных.
                        //Коротко: break из внутреннего while(true) прерывает только этот внутренний цикл; управление продолжается во внешнем цикле while (_rxRunning).
                        if (start == -1)
                        {
                            if (recvBuf.Count > 8192) recvBuf.Clear();
                            break;
                        }
                        //start + 1 — индекс, с которого начинается поиск (то есть первый байт после найденного START).
                        //b => b == FRAME_STOP — лямбда‑предикат: для каждого байта b проверяется равенство со значением FRAME_STOP(в твоём коде 0x05).
                        //Predicate<byte> predicate = delegate (byte b) { return b == FRAME_STOP; };
                        //int stop = recvBuf.FindIndex(start + 1, predicate); // можно и так
                        //Коротко и по делу — как работает List<T>.FindIndex (и его перегрузки) в C#.

                        //Что делает

                        //Просматривает элементы списка и возвращает индекс первого элемента, для которого предикат возвращает true.
                        //Если ничего не найдено — возвращает - 1.
                       //Подписи(самые используемые перегрузки)

                        //int FindIndex(Predicate<T> match)
                         //int FindIndex(int startIndex, Predicate<T> match)
                        //int FindIndex(int startIndex, int count, Predicate<T> match)
                         //Параметры

                        //match — Predicate<T>, т.е.делегат / метод с сигнатурой bool Predicate(T item).
                        //startIndex — индекс, с которого начать поиск(включительно).
                        //count — максимальное число элементов для проверки(начиная со startIndex).

                        int stop = recvBuf.FindIndex(start + 1, b => b == FRAME_STOP);

                        //Условие if (stop == -1) означает: стоп‑маркер после найденного START не найден (кадр не завершён).
                       // Ветвь if (start > 0) recvBuf.RemoveRange(0, start); удаляет все байты перед найденным START,
                       // то есть удаляются только «мусор»/ данные до самого START.Сам байт START остаётся в буфере(он станет на позицию 0 после удаления).
                        //Затем break; выходит из внутреннего парсера(while (true)) и управление возвращается во внешний цикл while (_rxRunning),
                        //где код ждёт новых данных.Когда придут дополнительные байты, парсер снова запустится и, найдя стоп, сможет завершить кадр.
                        if (stop == -1)
                        {
                            //Проверка start > 0 нужна, чтобы не вызывать RemoveRange(0, 0) — в случае start == 0 ничего не удаляется и START уже в начале буфера.
                            if (start > 0) recvBuf.RemoveRange(0, start);
                            break;
                        }

                        //вычисляет длину ASCII‑части кадра между START и STOP
                        int asciiLen = stop - (start + 1);
                        //если длина ASCII‑части меньше или равна нулю (то есть между START и STOP нет данных),
                        //то удаляет из буфера весь кадр (от начала до STOP включительно) и продолжает разбор следующего кадра
                        //(continue возвращает управление в начало внутреннего while (true)).
                        if (asciiLen <= 0)
                        {
                            recvBuf.RemoveRange(0, stop + 1);
                            continue;
                        }

                        byte[] asciiBytes = recvBuf.Skip(start + 1).Take(asciiLen).ToArray();
                        string asciiHex = System.Text.Encoding.ASCII.GetString(asciiBytes)
                                               .Replace("\r", "").Replace("\n", "").Trim()
                                               .ToUpperInvariant();

                        recvBuf.RemoveRange(0, stop + 1);

                        if (asciiHex.Length % 2 != 0)
                        {
                            DataReceived?.Invoke($"RX ERROR: odd hex length -> {asciiHex}");
                            continue;
                        }

                        byte[] frameBytes;
                        try
                        {
                            frameBytes = new byte[asciiHex.Length / 2];
                            for (int i = 0; i < frameBytes.Length; i++)
                                frameBytes[i] = Convert.ToByte(asciiHex.Substring(i * 2, 2), 16);
                        }
                        catch
                        {
                            DataReceived?.Invoke($"RX ERROR: invalid hex -> {asciiHex}");
                            continue;
                        }

                        bool chkOk = false;
                        if (frameBytes.Length >= 1)
                        {
                            int sum = 0;
                            for (int i = 0; i < frameBytes.Length - 1; i++) sum += frameBytes[i];
                            chkOk = ((byte)(sum & 0xFF)) == frameBytes[frameBytes.Length - 1];
                        }

                        string fullHex = BitConverter.ToString(frameBytes).Replace("-", "");
                        if (chkOk)
                        {
                            string payloadHex = frameBytes.Length > 1
                                ? BitConverter.ToString(frameBytes, 0, frameBytes.Length - 1).Replace("-", "")
                                : string.Empty;
                            DataReceived?.Invoke($"RX OK payload={payloadHex} full={fullHex}");
                        }
                        else
                        {
                            DataReceived?.Invoke($"RX CHK FAIL full={fullHex}");
                        }
                    } // parse loop
                } // outer loop
            }
            catch (Exception ex)
            {
                DataReceived?.Invoke($"Receiver error: {ex.Message}");
            }
            // гарантируем сброс флага при выходе
            finally
            {
                _rxRunning = false;
            }
        }

        // метод для остановки приёмника
        public void StopReceiver()
        {
            _rxRunning = false;
            try { bluetoothSocket?.Close(); } catch { }
        }
        ///////////////////////////////////////



        // событие для очистки данных
        public event Clear_EventHandler Clear_Devices;

        public async Task ClearData()
        {
            // вызываем событие очистки списка устройств
            Clear_Devices?.Invoke();
        }



        //событие для передачи данных
        public event Action Data_To_Send;
        //public async Task TransmitterData()
        //{
        //    // вызываем событие передачи данных
        //    Data_To_Send?.Invoke();
        //}


        public async Task TransmitterData()
        {
            // Проверяем, что сокет и его поток готовы к записи
            if (bluetoothSocket == null || bluetoothSocket.OutputStream == null)
                throw new InvalidOperationException("Bluetooth socket not ready");

            try
            {
                string asciiHex = "01010000240501012D"; // команда в ASCII HEX формате
                //Эти байты будут вставлены в начало и конец буфера как «сырые» (не ASCII). PIC ISR смотрит именно на такие «raw» старт/стоп.
                byte rawStart = 0x01; // стартовый байт
                byte rawStop = 0x05;  // стоповый байт
                // Преобразуем ASCII строку в байты
                // Преобразует строку в массив байт ASCII: каждый символ строки → один байт с кодом ASCII (например '0' -> 0x30).
                var asciiBytes = System.Text.Encoding.ASCII.GetBytes(asciiHex);

                // Формируем итоговый массив байт: rawStart + asciiBytes + rawStop - посылки длиной: 1 байт (start) + N ascii байтов + 1 байт (stop)
                // Создаём конечный буфер 
                byte[] toSend = new byte[1 + asciiBytes.Length + 1];
                // Записываем стартовый «сырой» байт в позицию 0.
                toSend[0] = rawStart;
                // Копируем ASCII байты в середину буфера (начиная с позиции 1).
                // Копируем ascii-байты в toSend, начиная с позиции 1 (после rawStart).
                // Array.Copy безопасен: если asciiBytes.Length соответствует, нижняя граница индексов ok.
                //  Важный момент: порядок байтов будет: [0] = 0x01, [1] = first ASCII char(0x30), [2] = next ASCII char(0x31), ... — это именно то, что нужно PIC
                Array.Copy(asciiBytes, 0, toSend, 1, asciiBytes.Length);
                // Записываем стоповый «сырой» байт в последнюю позицию.
                // Теперь буфер полностью сформирован и готов к отправке.
                toSend[toSend.Length - 1] = rawStop;
                //Получаем поток записи из сокета.
                var outStream = bluetoothSocket.OutputStream;
                // Пишем весь буфер в поток.
                // Асинхронная запись: пишет весь буфер в поток, не блокируя текущий поток (если поток поддерживает асинхронность).
                await outStream.WriteAsync(toSend, 0, toSend.Length);
                // Гарантирует, что все буфера потоков будут сброшены и данные реально отправлены (в пределах стека I/O).
                //Для некоторых реализаций Flush не обязателен, но безопасно его делать
                //Flush заставляет сбросить все данные из буферов «выше» в стек ввода-вывода и попытаться передать их дальше к следующему уровню (обычно в драйвер/сокет).
                //Если вы используете обёртки/ буферизаторы(BufferedStream, StreamWriter и т.п.), то Write может только положить данные в этот буфер; Flush гарантирует, что они действительно уйдут в нижележащий поток.
                //Flush не даёт гарантии, что данные дошли до удалённого устройства — он только продвигает данные вниз по стеку.Физическая доставка может быть прервана и не подтверждена — для подтверждения нужен протокол уровня приложения.
                await outStream.FlushAsync();

                // не обязательно логгировать через событие — пока пропускаем
            }
            catch (Exception ex)
            {
                // логируем или пробрасываем — пока покажем в DataReceived или пробросим
                // MainThread.BeginInvokeOnMainThread(() => DataReceived?.Invoke("Send error: " + ex.Message));
                throw; // или просто return, если не хотите бросать
            }
        }







        /////////////////////////

    }


    // ADDED: простой receiver для ActionStateChanged
    public class BroadcastReceiverStateChanged : BroadcastReceiver
    {
        public readonly AndroidBluetooth _owner;
        public BroadcastReceiverStateChanged(AndroidBluetooth owner) => _owner = owner;

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent?.Action == BluetoothAdapter.ActionStateChanged)
            {
                int state = intent.GetIntExtra(BluetoothAdapter.ExtraState, -1);
                bool enabled = (state == (int)State.On);
                // CHANGED: вызываем метод владельца, чтобы событие вызывалось из типа AndroidBluetooth
                _owner.RaiseBluetoothStateChanged(enabled);
            }
        }
    }



}















// ADDED: минимальный метод для проверки состояния адаптера
//public Task<bool> IsBluetoothEnabledAsync()
//{
//    bool enabled = _adapter != null && _adapter.IsEnabled;
//    return Task.FromResult(enabled);
//}











//public async Task<bool> OnOffBluetooth()
//{
//    if (_adapter == null) { return false; }//Проверяем, существует ли объект BluetoothAdapter.


//    if (!_adapter.IsEnabled)
//    {
//        // Проверка версии Android
//        bool isModernAndroid = Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu; // Android 13+
//        if (isModernAndroid)
//        {
//            var intent = new Intent(Settings.ActionBluetoothSettings);//это запрос к системе открыть настройки Bluetooth.
//            //эта строка указывает системе Android, что новое Activity, которое будет запущено этим интентом,
//            //должно быть создано в новом стеке задач ("new task").
//            //Это особенно важно, когда ты запускаешь интент из контекста,
//            //который не является Activity(например, из ApplicationContext).
//            intent.AddFlags(ActivityFlags.NewTask);
//            _context?.StartActivity(intent);

//            return true; // Пользователь должен вручную включить Bluetooth в настройках

//        }
//        else
//        {
//            // Программно включаем Bluetooth

//            // На Android < 13 (старые версии)
//            // Программно включаем Bluetooth
//            bool enabled = _adapter.Enable();
//            return enabled; // Если true — команда на включение отправлена
//        }
//        // Проверка версии Android



//    }

//    else
//    {

//        // Проверка версии Android
//        bool isModernAndroid = Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu; // Android 13+
//        if (isModernAndroid)
//        {
//            var intent = new Intent(Settings.ActionBluetoothSettings);//это запрос к системе открыть настройки Bluetooth.
//            //эта строка указывает системе Android, что новое Activity, которое будет запущено этим интентом,
//            //должно быть создано в новом стеке задач ("new task").
//            //Это особенно важно, когда ты запускаешь интент из контекста,
//            //который не является Activity(например, из ApplicationContext).
//            intent.AddFlags(ActivityFlags.NewTask);
//            _context?.StartActivity(intent);

//            return true; // Пользователь должен вручную включить Bluetooth в настройках

//        }
//        else
//        {
//            // Программно включаем Bluetooth

//            // На Android < 13 (старые версии)
//            // Программно включаем Bluetooth
//            bool enabled = _adapter.Disable();
//            return enabled; // Если true — команда на включение отправлена
//        }

//    }   
//    //return true;

//}





//Создание нового Receiver
//_receiver = new Device_Receiver(device =>
//{
//    DeviceDiscovered?.Invoke(device);
//}, () => DiscoveryFinished?.Invoke());



