


//using Android.Content;
using Microsoft.Maui.Controls;
using Scb_Electronmash.Models;
using System.Collections.ObjectModel;

namespace Scb_Electronmash;

public partial class Settings2Page : ContentPage
{

    private readonly IBluetooth_service _bluetoothService;
    private readonly ObservableCollection<Device_info> _devices = new ObservableCollection<Device_info>();
    private bool _suppressBluetoothToggle = false;


    // Флаг, чтобы предотвратить повторные тапы/многократные подключения
    private bool _isConnecting = false;


    public Settings2Page(IBluetooth_service bluetooth)
	{
		InitializeComponent();

        //связывает XAML-интерфейс с текущим классом (например, MainPage), чтобы иметь доступ к свойствам и командам напрямую из XAML.
        //BindingContext = this;


        _bluetoothService = bluetooth;
        // Привязываем коллекцию к CollectionView
        // Когда ты делаешь: DevicesCollectionView.ItemsSource = _devices; — то говоришь CollectionView: «Вот моя коллекция данных — отображай её».
        DevicesCollectionView.ItemsSource = _devices;
        //// Подписываемся на событие обнаружения устройства
        //_bluetoothService.DeviceDiscovered += OnDeviceDiscovered;
        //// (опционально) подписка на завершение, чтобы скрыть индикатор
        //_bluetoothService.DiscoveryFinished += OnDiscoveryFinished;

        ////  подписка на изменения состояния адаптера в реальном времени
        //_bluetoothService.BluetoothStateChanged += OnBluetoothStateChanged;

    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Подпишемся на события при появлении страницы (если ещё не подписаны)
        _bluetoothService.DeviceDiscovered += OnDeviceDiscovered;
        _bluetoothService.DiscoveryFinished += OnDiscoveryFinished;
        _bluetoothService.BluetoothStateChanged += OnBluetoothStateChanged;

        // Выставляем начальное состояние свитча (асинхронно, не блокируя UI)
        _ = InitializeBluetoothSwitchState();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Отписываемся от событий, чтобы не было утечек памяти и лишних вызовов
        _bluetoothService.DeviceDiscovered -= OnDeviceDiscovered;
        _bluetoothService.DiscoveryFinished -= OnDiscoveryFinished;
        _bluetoothService.BluetoothStateChanged -= OnBluetoothStateChanged; // ADDED
    }




    // Добавляем устройство в список (без дублей по MAC-адресу)
    private void OnDeviceDiscovered(Device_info device)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (device == null) return;
            // Предотвращаем дубликаты по Address
            Func<Device_info,bool> func = delegate (Device_info d)
            {
                return string.Equals(d.Address, device.Address, System.StringComparison.OrdinalIgnoreCase);
            };

            if (!_devices.Any(func)) { _devices.Add(device); }

            // Предотвращаем дубликаты по Address
            //if (!_devices.Any(d => string.Equals(d.Address, device.Address, System.StringComparison.OrdinalIgnoreCase)))
            //{
            //    _devices.Add(device);
            //}
        });
    }

    private void OnDiscoveryFinished()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            activityIndicator.IsRunning = false;
            activityIndicator.IsVisible = false;
            ScanSwitch.IsToggled = false;
        });
    }



    //private void OnBluetoothStateChanged(bool enabled)
    //{
    //    MainThread.BeginInvokeOnMainThread(() =>
    //    {
    //        _suppressBluetoothToggle = true;
    //        BluetoothSwitch.IsToggled = enabled;
    //        BluetoothSwitch.OnColor = enabled ? Colors.Green : Colors.Gray;
    //        BluetoothSwitch.ThumbColor = enabled ? Colors.White : Colors.DarkGray;
    //        _suppressBluetoothToggle = false;
    //    });
    //}

    private void OnBluetoothStateChanged(bool enabled)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _suppressBluetoothToggle = true;
            BluetoothSwitch.IsToggled = enabled;

            BluetoothSwitch.ThumbColor = Colors.White;
            BluetoothSwitch.OnColor = Colors.Green;
         //   BluetoothSwitch.BackgroundColor = Colors.Transparent;
            BluetoothSwitch.BackgroundColor = Colors.Transparent;
            _suppressBluetoothToggle = false;
        });
    }


    private async void OnBluetoothToggled(object sender, ToggledEventArgs e)
    {
        if (_suppressBluetoothToggle) return;

        bool requestedState = e.Value;
        bool success = await _bluetoothService.OnOffBluetooth(requestedState);

        if (!success)
        {
            // откатываем переключатель в UI
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _suppressBluetoothToggle = true;
                BluetoothSwitch.IsToggled = !requestedState;
                BluetoothSwitch.ThumbColor = Colors.White;
                BluetoothSwitch.OnColor = Colors.Green;
                BluetoothSwitch.BackgroundColor = Colors.Transparent;
                //BluetoothSwitch.BackgroundColor = Colors.LightGray;
                _suppressBluetoothToggle = false;
            });

            await DisplayAlert("Bluetooth", "Требуется ручное действие или нет разрешений", "OK");

            // опциональный короткий опрос: если пользователь всё же включил Bluetooth в настройках,
            // обновим UI автоматически в течение ~10–12 секунд
            _ = Task.Run(async () =>
            {
                const int maxTries = 12;
                for (int i = 0; i < maxTries; i++)
                {
                    bool enabled = false;
                    try { enabled = await _bluetoothService.IsBluetoothEnabledAsync(); } catch { enabled = false; }

                    if (enabled)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _suppressBluetoothToggle = true;
                            BluetoothSwitch.IsToggled = true;
                            BluetoothSwitch.ThumbColor = Colors.White;
                            BluetoothSwitch.OnColor = Colors.Green;
                            BluetoothSwitch.BackgroundColor = Colors.Transparent;
                            _suppressBluetoothToggle = false;
                        });
                        break;
                    }

                    await Task.Delay(1000);
                }
            });

            return;
        }

        // успех — обновляем визуально (цвет/фон)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BluetoothSwitch.ThumbColor = Colors.White;
            BluetoothSwitch.OnColor = Colors.Green;
            BluetoothSwitch.BackgroundColor = Colors.Transparent;
            //BluetoothSwitch.BackgroundColor = requestedState ? Colors.Transparent : Colors.LightGray;
        });

        await DisplayAlert("Bluetooth", requestedState ? "Включено" : "Выключено", "OK");
    }
    //private async void OnBluetoothToggled(object sender, ToggledEventArgs e)
    //{
    //    // e.Value — это новое состояние Switch:
    //    // true — пользователь включил (ползунок вправо)
    //    // false — выключил (ползунок влево)


    //    if (_suppressBluetoothToggle) return;
    //    bool b = await _bluetoothService.OnOffBluetooth(e.Value);

    //    if (!b)
    //        await DisplayAlert("Bluetooth", "Требуется ручное действие или нет разрешений", "OK");
    //    else
    //        await DisplayAlert("Bluetooth", e.Value ? "Включено" : "Выключено", "OK");
    //}

    

    private async Task InitializeBluetoothSwitchState()
    {
        bool enabled = false;
        try
        {
            enabled = await _bluetoothService.IsBluetoothEnabledAsync();
        }
        catch
        {
            enabled = false;
        }

        // при установке начального состояния
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _suppressBluetoothToggle = true;
            BluetoothSwitch.IsToggled = enabled;

            // всегда белый ползунок (thumb)
            BluetoothSwitch.ThumbColor = Colors.White;

            // трек: зелёный в ON; доверим системе отрисовку OFF-трека (без прямоугольника)
            BluetoothSwitch.OnColor = Colors.Green;
            BluetoothSwitch.BackgroundColor = Colors.Transparent; // <-- прозрачный вместо светлого квадрата

            _suppressBluetoothToggle = false;
        });

    }


    private async void OnScanToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            // Очистка предыдущих результатов (опционально)
            _devices.Clear();

            // Попытка запустить сканирование
            bool b = await _bluetoothService.StartScanningAsync();

            if (b)
            {
                // Показываем индикатор и меняем цвет переключателя только если сканирование реально стартовало
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    activityIndicator.IsVisible = true;
                    activityIndicator.IsRunning = true;
                    //BluetoothSwitch.OnColor = Colors.Green;
                    //BluetoothSwitch.ThumbColor = Colors.White;
                });

                await DisplayAlert("Scan", "Включено", "OK");
            }
            else
            {
                // Сканирование не стартовало — скрываем индикатор и возвращаем переключатель в OFF
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    activityIndicator.IsVisible = false;
                    activityIndicator.IsRunning = false;
                    ScanSwitch.IsToggled = false;
                });

                await DisplayAlert("Scan", "Нет разрешений на сканирование ", "OK");
            }
        }
        else
        {
            // Скрываем индикатор
            MainThread.BeginInvokeOnMainThread(() =>
            {
                activityIndicator.IsVisible = false;
                activityIndicator.IsRunning = false;
            });

          //  await DisplayAlert("Scan", "Выключено", "OK");
            
        }
    }











    //private async void OnDeviceSelected(object sender, SelectionChangedEventArgs e)
    //{
    //    //e.CurrentSelection — коллекция текущих выбранных элементов (в CollectionView при SelectionMode="Single" там 0 или 1 элемент).
    //    //FirstOrDefault() берёт первый элемент или null, если ничего не выбрано.
    //    //as Device_info — приведение типа; если тип не совпадает, selected будет null.
    //    var selected = e.CurrentSelection.FirstOrDefault() as Device_info;
    //    if (selected == null)
    //        return;

    //    // Пример действия: показать данные выбранного устройства
    //    await DisplayAlert("Устройство выбрано", $"{selected.Name}\n{selected.Address}", "OK");

    //    // Попытка подключения к выбранному устройству - через ваш сервис Bluetooth
    //    bool conneckted = await _bluetoothService.ConnectToDeviceAsync(selected);


    //    if (conneckted)
    //    {
    //        await DisplayAlert("Подключение", $"Успешно подключено к {selected.Name}", "OK");
    //    }
    //    else
    //    {
    //        await DisplayAlert("Подключение", $"Не удалось подключиться к {selected.Name}", "OK");
    //    }




    //    // Сбрасываем выделение, чтобы можно было выбрать элемент снова
    //    if (sender is CollectionView cv)
    //        cv.SelectedItem = null;
    //    else
    //        DevicesCollectionView.SelectedItem = null;
    //}



    // Код аккуратно получает Device_info через BindingContext элемента.




//Фрагмент: <Grid.GestureRecognizers> <TapGestureRecognizer Tapped = "OnItemTapped" CommandParameter="{Binding .}" /> </Grid.GestureRecognizers>

//Что это организует(по пунктам)

//Коллекция GestureRecognizers у Grid
//Grid.GestureRecognizers — это коллекция жестов(GestureRecognizers), которые «вешаются» на сам Grid.
//То есть ты говоришь: «этот Grid должен реагировать на определённые жесты» (в данном случае — на тап).
//TapGestureRecognizer — распознаёт простое нажатие(тап)
//TapGestureRecognizer реагирует на одиночный тап(или двойной, если настроить NumberOfTapsRequired).
//Когда пользователь нажмёт по области Grid, распознаётся событие Tap и вызывается либо привязанная команда, либо обработчик события Tapped.
//Tapped="OnItemTapped" — связывает событие с обработчиком в code‑behind
//При срабатывании жеста будет вызван метод OnItemTapped в твоём Settings2Page.xaml.cs.
//Сигнатура обработчика обычно: private async void OnItemTapped(object sender, TappedEventArgs e).
//sender — элемент, на котором висит жест(чаще всего этот Grid).
//e — аргументы, в т.ч.e.Parameter(если задан CommandParameter).
//CommandParameter="{Binding .}" — передаёт текущий объект в обработчик/команду
//{Binding.} внутри ItemTemplate означает «передай текущий BindingContext ячейки» — то есть сам объект из ItemsSource(Device_info).
//CommandParameter может быть использован в двух сценариях:
//если используется Command(MVVM) — параметр попадёт в Execute(object parameter);
//если используется событие Tapped — платформенно e.Parameter часто содержит этот параметр (но не 100% гарантированно на всех версиях/платформах), поэтому в обработчике лучше проверять и e.Parameter, и sender.BindingContext.



    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        Device_info device = null;

        // 1) Попробуем получить из CommandParameter (e.Parameter)
        if (e?.Parameter is Device_info p)
        {
            device = p;
            await DisplayAlert("Device from e.Parameter", $"{device.Name}\n{device.Address}", "OK");
        }

        // 2) Если нет — попробуем через sender.BindingContext (чаще работает)
        //sender — это обычно Grid, на котором висит жест
        //VisualElement — базовый класс для всех визуальных элементов в MAUI
        //BindingContext содержит привязанный объект (в данном случае Device_info)


        if (device == null && sender is VisualElement ve && ve.BindingContext is Device_info ctx)
        {
            device = ctx;
            await DisplayAlert("Device from sender.BindingContext", $"{device.Name}\n{device.Address}", "OK");  
        }

        if (device == null)
            return;

        // Пока что просто показать, что элемент выбран
        await DisplayAlert("Устройство выбрано", $"{device.Name}\n{device.Address}", "OK");

        // Попытка подключения к выбранному устройству - через ваш сервис Bluetooth
        bool conneckted = await _bluetoothService.ConnectToDeviceAsync(device);

        if (conneckted)
        {
            await DisplayAlert("Подключение", $"Успешно подключено к {device.Name}", "OK");
        }
        else
        {
            await DisplayAlert("Подключение", $"Не удалось подключиться к {device.Name}", "OK");
        }




    }



    //У тебя ItemsSource = _devices — это коллекция объектов Device_info.
    //    В XAML у тебя DataTemplate, который описывает одну «ячейку»: Grid → StackLayout → два Label.
    //    На сам Grid навешан TapGestureRecognizer.
    //Что происходит, когда CollectionView рисует список
    //    Для каждого объекта device в _devices фреймворк создаёт один визуальный блок (одну «ячейку») по твоему DataTemplate.
    //    Для каждой созданной ячейки MAUI автоматически делает: cell.BindingContext = device где cell — это корневой визуальный элемент шаблона(в твоём случае Grid).
    //То есть:
    //первая ячейка: Grid1.BindingContext = _devices[0] (первый Device_info)
    //вторая ячейка: Grid2.BindingContext = _devices[1]
    //и т.д.
    //Теперь про sender в обработчике
    //Ты навесила жест на Grid, поэтому при тапе обычно: sender == тот самый Grid для ячейки, по которой тапнули.
    //Значит sender — это Grid1 или Grid2(в зависимости от того, по какой ячейке тапнули).
    //И как это связано с BindingContext
    //Поскольку при создании ячейки фреймворк установил Grid.BindingContext = соответствующий Device_info, то: (sender as VisualElement).BindingContext вернёт именно объект Device_info, связанный с этой ячейкой.
    //То есть код получает модель, которая «лежит за» данной визуальной ячейкой.
    //Почему условие if делает именно это Строка: if (device == null && sender is VisualElement ve && ve.BindingContext is Device_info ctx)
    //разбивается так:

    //device == null — мы ещё не нашли устройство из e.Parameter, поэтому пытаемся другим способом.
    //sender is VisualElement ve — проверяем, что sender можно рассматривать как VisualElement (Grid — это VisualElement). Если да, в локальную ve кладём sender приведённый к VisualElement.
    //ve.BindingContext is Device_info ctx — проверяем, что у этого визуального элемента BindingContext — это объект типа Device_info; если да, создаём локальную переменную ctx с этим объектом.
    //Если все три проверки прошли, внутри if у тебя есть ctx — именно тот Device_info, связанный с нажатой ячейкой.







}

















//// ADDED: для начального состояния Bluetooth при загрузке страницы
//_ = Task.Run(async () =>
//{
//    bool enabled = false;
//    try
//    {
//        enabled = await _bluetoothService.IsBluetoothEnabledAsync();
//    }
//    catch
//    {
//        enabled = false;
//    }

//    MainThread.BeginInvokeOnMainThread(() =>
//    {
//        BluetoothSwitch.IsToggled = enabled;
//        BluetoothSwitch.OnColor = enabled ? Colors.Green : Colors.Gray;
//        BluetoothSwitch.ThumbColor = enabled ? Colors.White : Colors.DarkGray;
//    });
//});










// Подписка на событие обнаружения устройства
//_bluetoothService.DeviceDiscovered += delegate (Device_info device)
//{
//    // Главный поток для UI, чтоб в UI было изменение коллекции DiscoveredDevices
//    MainThread.BeginInvokeOnMainThread(delegate ()
//    {

//        // Показываем Alert с названием устройства, если оно есть
//        DisplayAlert("Bluetooth", $"Найдено новое устройство: {device.Name ?? "Имя неизвестно"}", "OK");

//        //// Добавление найденного устройства в коллекцию
//        //DevicesListView.ItemsSource = null; // Сброс источника данных
//        //DevicesListView.ItemsSource = DevicesListView.ItemsSource.Cast<Device_info>().Append(device).ToList();
//    });
//};  




//private async void OnScanToggled(object sender, ToggledEventArgs e)
//{

//    if (e.Value)
//    {



//        // Включить Bluetooth
//        await DisplayAlert("Scan", "Включено", "OK");
//        BluetoothSwitch.OnColor = Colors.Green;
//        BluetoothSwitch.ThumbColor = Colors.White;

//        // Главный поток для UI, чтоб в UI было изменение коллекции DiscoveredDevices
//        MainThread.BeginInvokeOnMainThread(delegate ()
//        {
//            // обработка крутилки
//            activityIndicator.IsVisible = true;//элемент виден пользователю.
//            activityIndicator.IsRunning = true;//индикатор крутится
//        });
//        // Запуск сканирования
//        bool b = await _bluetoothService.StartScanningAsync();


//        if (!b) { await DisplayAlert("Scan", "Нет разрешений на сканирование ", "OK"); }


//    }
//    else
//    {

//        // Главный поток для UI, чтоб в UI было изменение коллекции DiscoveredDevices
//        MainThread.BeginInvokeOnMainThread(delegate ()
//        {
//            // обработка крутилки
//            activityIndicator.IsVisible = false;//элемент виден пользователю.
//            activityIndicator.IsRunning = false;//индикатор крутится
//        });





//        // Выключить Bluetooth
//        await DisplayAlert("Scan", "Выключено", "OK");
//        BluetoothSwitch.OnColor = Colors.Gray;
//        BluetoothSwitch.ThumbColor = Colors.DarkGray;
//    }


//    //bool b = await _bluetoothService.StartScanningAsync();
//    //if (!b) { await DisplayAlert("Bluetooth", "Нет разрешений или Bluetooth выключен/не найден", "OK"); }
//}

// Замените ваш текущий OnScanToggled этим минимальным вариантом (без try/catch).






















//private async void OnBluetoothToggled(object sender, ToggledEventArgs e)
//{
//    if (e.Value)
//    {

//         BluetoothSwitch.OnColor = Colors.Green;
//         BluetoothSwitch.ThumbColor = Colors.White;

//        bool b = await _bluetoothService.OnOffBluetooth();
//        if (!b) { await DisplayAlert("Bluetooth", "Нет разрешений или Bluetooth выключен/не найден", "OK"); }
//        else { await DisplayAlert("Bluetooth", "Включено", "OK"); }
//    }
//    else
//    {
//        // Выключить Bluetooth
//        BluetoothSwitch.OnColor = Colors.Gray;
//        BluetoothSwitch.ThumbColor = Colors.DarkGray;
//        bool b = await _bluetoothService.OnOffBluetooth();
//        if (!b) { await DisplayAlert("Bluetooth", "Нет разрешений или Bluetooth выключен/не найден", "OK"); }
//        else { await DisplayAlert("Bluetooth", "Выключен", "OK"); }

//        //await  DisplayAlert("Bluetooth", "Выключено", "OK");

//    }
//    // await DisplayAlert("Bluetooth", isOn ? "Включено" : "Выключено", "OK");
//}





//MainThread.BeginInvokeOnMainThread(() =>
//{
//    _suppressBluetoothToggle = true;
//    BluetoothSwitch.IsToggled = enabled;
//    BluetoothSwitch.OnColor = enabled ? Colors.Green : Colors.Gray; // ваш цвет
//    BluetoothSwitch.ThumbColor = enabled ? Colors.White : Colors.DarkGray;
//    _suppressBluetoothToggle = false;
//});

