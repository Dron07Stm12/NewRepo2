using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Microsoft.Maui.Controls;
using Scb_Electronmash.Platforms.Android;

namespace Scb_Electronmash
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
#pragma warning disable CS8765 // Допустимость значений NULL для типа параметра не соответствует переопределенному элементу (возможно, из-за атрибутов допустимости значений NULL).
        protected override async void OnCreate(Bundle savedInstanceState)
#pragma warning restore CS8765 // Допустимость значений NULL для типа параметра не соответствует переопределенному элементу (возможно, из-за атрибутов допустимости значений NULL).
        {
            base.OnCreate(savedInstanceState);
            // Ждём, пока пользователь ответит на запрос разрешений
            bool granted = await BluetoothPermissionsHelper.RequestBluetoothPermissionsAsync(this);
            // INSERT LOG HERE (optional)
            Android.Util.Log.Info("BTPerms", $"MainActivity: RequestBluetoothPermissionsAsync returned: {granted}");

            if (granted)
            {
                // Можно работать с Bluetooth
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
                Toast.MakeText(this, "Bluetooth разрешения получены!", ToastLength.Short).Show();
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.

            }
            else
            {
                // Пользователь отказал — покажи предупреждение или ограничь функционал
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
                Toast.MakeText(this, "Bluetooth разрешения НЕ получены!", ToastLength.Short).Show();
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
            }
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
#pragma warning disable CA1416 // Проверка совместимости платформы
            //Когда можно не вызывать?
            //Только если ты точно уверен, что ничего кроме твоего собственного кода обработкой разрешений не занимается.
            //Но в большинстве случаев вызывать базовую реализацию — хорошая практика.
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);// уведомляет систему и библиотеки - стандартный вызов 

#pragma warning restore CA1416 // Проверка совместимости платформы
            BluetoothPermissionsHelper.OnRequestPermissionsResult(requestCode, permissions, grantResults);// моя  логика
        }




    }
}
//MainActivity
//   |
//   |-- await BluetoothPermissionsHelper.RequestBluetoothPermissionsAsync(this)
//   |         |
//   |         |--- показывает диалог разрешений
//   |         |--- ждёт задачу (_tcs.Task)
//   | 
//   |-- [пользователь отвечает]
//   |
//Android вызывает OnRequestPermissionsResult
//   |
//   |-- вызывается BluetoothPermissionsHelper.OnRequestPermissionsResult
//   |         |
//   |         |--- завершает задачу (_tcs.TrySetResult)
//   |
//   |-- await продолжает работуMainActivity
//   |
//   |-- await BluetoothPermissionsHelper.RequestBluetoothPermissionsAsync(this)
//   |         |
//   |         |--- показывает диалог разрешений
//   |         |--- ждёт задачу (_tcs.Task)
//   | 
//   |-- [пользователь отвечает]
//   |
//Android вызывает OnRequestPermissionsResult
//   |
//   |-- вызывается BluetoothPermissionsHelper.OnRequestPermissionsResult
//   |         |
//   |         |--- завершает задачу (_tcs.TrySetResult)
//   |
//   |-- await продолжает работу