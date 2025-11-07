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
        }

        private async void OnCounterClicked(object? sender, EventArgs e)
        {
           

            bool b = await _bluetoothService.StartScanningAsync();
            if (!b) { await DisplayAlert("Bluetooth", "Нет разрешений или Bluetooth выключен/не найден", "OK"); }



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