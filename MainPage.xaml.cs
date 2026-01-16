
#if ANDROID
using Scb_Electronmash.Platforms.Android;
#endif
using System.Diagnostics;
using System.Threading.Tasks;

namespace Scb_Electronmash
{
    public partial class MainPage : ContentPage
    {
       
        private readonly IBluetooth_service _bluetoothService;



        // Внутри MainPage класса — поля
        // Добавить внутрь класса MainPage (например сразу после поля _bluetoothService)
        private readonly Dictionary<string, string> _commands = new Dictionary<string, string>
        {

            // Ключи точно соответствуют CommandParameter в XAML: "1|Device_Name"    первая страница
            ["1|Device_Name"] = "0101000021000080A3",


            // Ключи точно соответствуют CommandParameter в XAML: "2|CodeObject1"  вторая страница 
            ["2|CodeObject1"] = "010100002200010429",
            ["2|AccessPoint1"] ="01010000220002183E",
            ["2|AccessPoint2"] ="01010000220003183F",
            ["2|ServerPort1"] = "010100002200040830",
            ["2|ServerPort2"] = "010100002200050831",
            ["2|ipServer1"]   = "01010000220006103A",
            ["2|ipServer2"]   = "01010000220007103B",
            ["2|SMSPhone1"]   = "010100002200081844",
            //["2|ServerPort6"] = "010100002200090835",
            //["2|ServerPort7"] = "0101000022000A0836",
            //["2|ServerPort8"] = "0101000022000B0837",

            // Ключи точно соответствуют CommandParameter в XAML: "4|tokshelyf1" ... "4|tokshelyf16"      
            ["4|tokshelyf1"] = "01010000240501012D",
            ["4|tokshelyf2"] = "01010000240502012E",
            ["4|tokshelyf3"] = "01010000240503012F",
            ["4|tokshelyf4"] = "010100002405040130",
            ["4|tokshelyf5"] = "010100002405050131",
            ["4|tokshelyf6"] = "010100002405060132",
            ["4|tokshelyf7"] = "010100002405070133",
            ["4|tokshelyf8"] = "010100002405080134",
            ["4|tokshelyf9"] = "010100002405090135",
            ["4|tokshelyf10"] = "0101000024050A0136",
            ["4|tokshelyf11"] = "0101000024050B0137",
            ["4|tokshelyf12"] = "0101000024050C0138",
            ["4|tokshelyf13"] = "0101000024050D0139",
            ["4|tokshelyf14"] = "0101000024050E013A",
            ["4|tokshelyf15"] = "0101000024050F013B",
            ["4|tokshelyf16"] = "01010000240510013C",
        };
        //<- TX	 01010000240501012D
        //-> RX	 01010000240501017A00
        //<- TX	 01010000240502012E
        //-> RX	 01010000240502017A00
        //<- TX  01010000240503012F
        //-> RX  01010000240503017A00
        //<- TX	 010100002405040130
        //-> RX	 01010000240504017B00
        //<- TX  010100002405050131
        //-> RX  01010000240505017A00
        //<- TX	 010100002405060132
        //-> RX	 01010000240506017A00
        //<- TX  010100002405070133
        //-> RX  01010000240507017A00
        //<- TX	 010100002405080134
        //-> RX	 01010000240508017A00
        //<- TX  010100002405090135
        //<- TX  010100002405090135
        //-> RX  01010000240509015400
        //<- TX  0101000024050A0136
        //-> RX  0101000024050A015300
        //<- TX	 0101000024050B0137
        //-> RX	 0101000024050B015300
        //<- TX  0101000024050C0138
        //-> RX  0101000024050C015300
        //<- TX	 0101000024050D0139
        //-> RX	 0101000024050D017A00
        //<- TX  0101000024050E013A
        //-> RX  0101000024050E017A00
        //<- TX	 0101000024050F013B
        //-> RX	 0101000024050F017A00
        //<- TX  01010000240510013C
        //-> RX  01010000240510017A00


        // Поле для хранения последней выбранной команды (ASCII HEX)
        private string? _lastSelectedCommand = null;

        private string? _stringentry = null;


        public MainPage(IBluetooth_service bluetooth)
        {
            InitializeComponent();

            // Запускаем Foreground Service
            //Когда приложение входит в главную страницу (MainPage), выполняется код, который создаёт Intent.
            //Этот Intent сообщает системе Android: "Запусти Foreground Service (UiPriorityService)", создавая запрос на запуск.
#if ANDROID
            var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(UiPriorityService));
            Android.App.Application.Context.StartService(intent);
#endif





            _bluetoothService = bluetooth;
          //  BindingContext = this;

            // Подписываемся на событие получения данных из Bluetooth-сервиса
            _bluetoothService.DataReceived += OnDataReceived;

            // Подписываемся на событие очистки данных
            _bluetoothService.Clear_Devices += Clear_D;


        }

        // кнопка по приему данных
        private async void OnCounterClicked(object? sender, EventArgs e)
        {     
            await _bluetoothService.ReceiverData();
        }

        // Обработчик события DataReceived: обновляет label4 в UI потоке
        // Обновление label4 только на главном потоке!
        private void OnDataReceived(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                label4.Text += $"{Environment.NewLine}{message}";
                scrollView.ScrollToAsync(label4, ScrollToPosition.End, true);
            });
        }


        // кнопка по приему данных
        private async void OnCounterClicked_Tx(object? sender, EventArgs e)
        {
            var btn = sender as Button;
            try
            {
                if (btn != null) btn.IsEnabled = false; // блокируем кнопку на время отправки


                // защита: команда должна быть выбрана
                if (string.IsNullOrWhiteSpace(_lastSelectedCommand))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Transmitter", "Команда не выбрана. Сначала нажмите на обьект.", "OK"));
                    return;
                }

                // await _bluetoothService.TransmitterData();
                // вызов перегрузки с параметром (минимальное изменение)
                await _bluetoothService.TransmitterData(_lastSelectedCommand);

                await DisplayAlert("Transmitter", "Tx sent", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Transmitter", "Send failed: " + ex.Message, "OK");
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        // кнопка по очистке данных
        private async void OnClear_Data_Dev(object? sender, EventArgs e)
        {
            await _bluetoothService.ClearData();
            //label4.Text = string.Empty;
            //await _bluetoothService.Clear_Devices();

        }

        public void Clear_D() {

         //   label4.Text = string.Empty;

            // Выводим сообщение
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DisplayAlert("Data", "Clear", "OK");
                label4.Text = "";
            });



        }


        private void MainCarousel_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            // Пример: показать в label4 текст текущей страницы
            if (e?.CurrentItem is string text)
            {
                label4.Text = text;
            }
        }

        // Обработчик тапов для "ток шлейфа 1" / "ток шлейфа 2"
        private async void OnShleyfTapped(object sender, EventArgs e)
        {
            try
            {
                string param = null;
                if (sender is TapGestureRecognizer tap)
                    param = tap.CommandParameter?.ToString();
                else if (sender is Microsoft.Maui.Controls.View view)
                    param = view.GestureRecognizers?.OfType<TapGestureRecognizer>().FirstOrDefault()?.CommandParameter?.ToString();

                var parts = (param ?? "(no param)").Split('|');
                var page = parts.Length > 0 ? parts[0] : "?";
                var id = parts.Length > 1 ? parts[1] : "?";

                string key = $"{page}|{id}";

                // Попытка взять команду из словаря
                if (_commands.TryGetValue(key, out var asciiHex))
                {
                    _lastSelectedCommand = asciiHex;
                    Debug.WriteLine($"Selected key={key}, asciiHex={asciiHex}");
                    // Показываем короткое уведомление пользователю, что команда выбрана
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Команда выбрана", $"Ключ: {key}\nКоманда: {asciiHex}", "OK"));
                }
                else
                {
                    _lastSelectedCommand = null;
                    Debug.WriteLine($"Key not found in commands: {key}");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Не найдена команда", $"Ключ {key} не задан в словаре", "OK"));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnShleyfTapped exception: {ex}");
            }
        }

        //тапет для записи данных

        // Обработчик тапов для записи данных с учетом CommandParameter
        private async void OnPage1Tapped(object sender, EventArgs e)
        {
            try
            {
                string param = null;
                // Способы извлечения CommandParameter
                if (sender is TapGestureRecognizer tap)
                    param = tap.CommandParameter?.ToString();
                else if (sender is Microsoft.Maui.Controls.View view)
                    param = view.GestureRecognizers?.OfType<TapGestureRecognizer>().FirstOrDefault()?.CommandParameter?.ToString();

                var parts = (param ?? "(no param)").Split('|');
                var page = parts.Length > 0 ? parts[0] : "?";
                var id = parts.Length > 1 ? parts[1] : "?";

                string key = $"{page}|{id}";

                // Попытка взять команду из словаря
                if (!_commands.TryGetValue(key, out var asciiHex))
                {
                    Debug.WriteLine($"Key not found in commands: {key}");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Ошибка", $"Команда для ключа {key} не найдена в словаре", "OK"));
                    return;
                }

                Debug.WriteLine($"Selected key = {key}, asciiHex = {asciiHex}");

                // Парсим команду для извлечения индекса и субиндекса
                byte[] index;
                byte subindex;

                try
                {
                    var parsedData = ParseCommand(asciiHex);
                    index = parsedData.index;
                    subindex = parsedData.subindex;

                    Debug.WriteLine($"Parsed Command - Index: {BitConverter.ToString(index)}, Subindex: {subindex:X2}");

                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Команда выбрана", $"Index: {BitConverter.ToString(index)}\nSubindex: {subindex:X2}", "OK"));
                }
                catch (Exception parseEx)
                {
                    Debug.WriteLine($"ParseCommand Exception: {parseEx}");
                    await DisplayAlert("Ошибка", $"Ошибка разбора команды: {parseEx.Message}", "OK");
                    return;
                }

                // Получение значения из Entry
                var parent = sender as VisualElement;
                while (parent != null && !(parent is Grid))
                    parent = parent.Parent as VisualElement;

                string value = null;
                if (parent is Grid grid)
                {
                    var entry = grid.Children.OfType<Entry>().FirstOrDefault(en => Grid.GetColumn(en) == 1);
                    value = entry?.Text?.Trim();
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Ошибка", "Поле ввода пустое", "OK"));
                    Debug.WriteLine("Entry value is empty or null.");
                    return;
                }

                Debug.WriteLine($"Entry Value: {value}");

                // Простая валидация/ограничение длины
                if (value.Length > 250)
                {
                    await DisplayAlert("Ошибка", "Слишком длинная строка", "OK");
                    return;
                }

                try
                {
                    // Передаем данные
                    await _bluetoothService.TransmitterData_write(value, 0x02, 0x01, index, subindex, true);

                    Debug.WriteLine($"Data Sent - Value: {value}, Index: {BitConverter.ToString(index)}, Subindex: {subindex:X2}");

                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Отправлено", $"Value: {value}\nIndex: {BitConverter.ToString(index)}\nSubindex: {subindex:X2}", "OK"));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error sending data: {ex}");
                    await DisplayAlert("Ошибка", "Не удалось отправить данные", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnPage1Tapped Exception: {ex}");
            }
        }


        // Метод для парсинга команды и извлечения индекса и субиндекса
        private static (byte[] index, byte subindex) ParseCommand(string command)
        {
            // Проверка на минимальную длину строки (не менее 13 символов для индекса + субиндекса)
            if (string.IsNullOrEmpty(command) || command.Length < 13)
                throw new ArgumentException("Строка команды должна быть длиной не менее 13 символов");

            // Извлекаем индекс (4 байта) как HEX-строку и преобразуем в массив байтов
            var indexHex = command.Substring(4, 8); // Символы с позиции 4 (включительно) — 8 символов после заголовка
            byte[] index = HexStringToBytes(indexHex);

            // Извлекаем субиндекс (1 байт) как HEX-строку и преобразуем в 1 байт
            var subindexHex = command.Substring(12, 2); // Символы с позиции 12 (включительно) — 2 символа выделенного субиндекса
            byte subindex = byte.Parse(subindexHex, System.Globalization.NumberStyles.HexNumber);

            return (index, subindex);
        }

        // Утилита преобразования HEX-строки в массив байтов
        private static byte[] HexStringToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
            hex = hex.Replace(" ", "");
            if (hex.Length % 2 != 0) throw new ArgumentException("Hex string must have even length");
            return Enumerable.Range(0, hex.Length / 2)
                             .Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16))
                             .ToArray();
        }
        ////////////////

    }
}



















//int count = 0;
//count++;

//if (count == 1)
//    CounterBtn.Text = $"Clicked {count} time";
//else
//    CounterBtn.Text = $"Clicked {count} times";

//SemanticScreenReader.Announce(CounterBtn.Text);



//private async void OnPage1Tapped(object sender, EventArgs e)
//{
//    try
//    {
//        // Получаем значение из Entry как раньше...
//        var source = (sender as TapGestureRecognizer)?.Parent as VisualElement ?? sender as VisualElement;
//        var parent = source;
//        while (parent != null && !(parent is Grid)) parent = parent.Parent as VisualElement;

//        string value = null;
//        if (parent is Grid grid)
//        {
//            var entry = grid.Children.OfType<Entry>().FirstOrDefault(en => Grid.GetColumn(en) == 1)
//                        ?? grid.Children.OfType<Entry>().FirstOrDefault();
//            value = entry?.Text?.Trim();
//        }

//        if (string.IsNullOrWhiteSpace(value))
//        {
//            await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Ошибка", "Поле пустое", "OK"));
//            //   await _bluetoothService.TransmitterData();
//            return;
//        }

//        // Простая валидация/ограничение длины
//        if (value.Length > 250)
//        {
//            await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Ошибка", "Слишком длинная строка", "OK"));
//            return;
//        }

//        // Откл. UI / защита от повторных тапов — пример: отключаем sender, можно кастомизировать
//        try { (sender as VisualElement).IsEnabled = false; } catch { }

//        byte usedSubindex = 0x02;

//        await Task.Delay(100);
//        try
//        {
//            await _bluetoothService.TransmitterData_write(value, 0x02, 0x01, null, usedSubindex, true);

//            await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("OK", $"Отправлено: {value}", "OK"));
//        }
//        catch (ArgumentOutOfRangeException ex)
//        {
//            await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Ошибка", "Данные слишком длинные", "OK"));
//        }
//        catch (InvalidOperationException ex)
//        {
//            await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Ошибка", "Bluetooth не готов", "OK"));
//        }
//        catch (Exception ex)
//        {
//            await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Ошибка", "Не удалось отправить данные", "OK"));
//            System.Diagnostics.Debug.WriteLine(ex);
//        }
//        finally
//        {
//            try { (sender as VisualElement).IsEnabled = true; } catch { }
//        }
//    }
//    catch (Exception ex)
//    {
//        System.Diagnostics.Debug.WriteLine($"OnPage1Tapped exception: {ex}");
//    }
//}

