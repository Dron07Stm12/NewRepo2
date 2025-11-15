using System.Threading.Tasks;

namespace Scb_Electronmash
{
    public partial class MainPage : ContentPage
    {
       
        private readonly IBluetooth_service _bluetoothService;
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

                await _bluetoothService.TransmitterData();

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



        // андрей начало изменений
        // Обработчик тапов для "ток шлейфа 1" / "ток шлейфа 2"
        private async void OnShleyfTapped(object sender, EventArgs e)
        {
            if (sender is TapGestureRecognizer tap)
            {
                var param = tap.CommandParameter?.ToString() ?? "(no param)";
                var parts = param.Split('|');
                var page = parts.Length > 0 ? parts[0] : "?";
                var id = parts.Length > 1 ? parts[1] : "?";

                // Убедимся, что вызов DisplayAlert в UI-потоке
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Нажато", $"Страница {page}: {id}", "OK");
                });
            }
        }
        // андрей конец изменений







    }
}



















//int count = 0;
//count++;

//if (count == 1)
//    CounterBtn.Text = $"Clicked {count} time";
//else
//    CounterBtn.Text = $"Clicked {count} times";

//SemanticScreenReader.Announce(CounterBtn.Text);