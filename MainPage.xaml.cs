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

            // Ключи точно соответствуют CommandParameter в XAML: "1|Device_Name"      
            ["1|Device_Name"] = "0101000021000080A3",



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


        public MainPage(IBluetooth_service bluetooth)
        {
            InitializeComponent();
            _bluetoothService = bluetooth;


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
                        DisplayAlert("Transmitter", "Команда не выбрана. Сначала нажмите на шлейф.", "OK"));
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
        //private async void OnShleyfTapped(object sender, EventArgs e)
        //{
           

        //    try
        //    {
        //        Debug.WriteLine($"OnShleyfTapped invoked; sender type = {(sender?.GetType().Name ?? "(null)")}");

        //        string param = null;

        //        // Если sender — сам TapGestureRecognizer
        //        if (sender is TapGestureRecognizer tap)
        //        {
        //            param = tap.CommandParameter?.ToString();
        //        }
        //        // Если sender — View (Border и т.п.), берём его GestureRecognizers
        //        else if (sender is Microsoft.Maui.Controls.View view)
        //        {
        //            var tg = view.GestureRecognizers?.OfType<TapGestureRecognizer>().FirstOrDefault();
        //            param = tg?.CommandParameter?.ToString();
        //        }

        //        Debug.WriteLine($"OnShleyfTapped param = {(param ?? "(null)")}");

        //        var parts = (param ?? "(no param)").Split('|');
        //        var page = parts.Length > 0 ? parts[0] : "?";
        //        var id = parts.Length > 1 ? parts[1] : "?";

        //        // Показываем алерт на UI-потоке
        //        await MainThread.InvokeOnMainThreadAsync(() =>
        //            DisplayAlert("Нажато", $"Страница {page}: {id}", "OK"));
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"OnShleyfTapped exception: {ex}");
        //    }


        //}


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


        private async void OnPage1Tapped(object sender, EventArgs e)
        {

            await MainThread.InvokeOnMainThreadAsync(() =>
                           DisplayAlert("Команда выбрана", $"Ключ: \nКоманда: ", "OK"));

        }






     }
}



















//int count = 0;
//count++;

//if (count == 1)
//    CounterBtn.Text = $"Clicked {count} time";
//else
//    CounterBtn.Text = $"Clicked {count} times";

//SemanticScreenReader.Announce(CounterBtn.Text);