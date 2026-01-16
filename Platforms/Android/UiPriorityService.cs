using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
//using Resource = Microsoft.Maui.Controls.Resource;

namespace Scb_Electronmash.Platforms.Android
{


    //Foreground Service — это специальный сервис Android, который выполняет важные задачи,
    //которые критически важны для пользователя, и работает даже тогда, когда приложение свернуто или находится в фоне.

    [Service(ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class UiPriorityService : Service
    {
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            // Создаём уведомление для Foreground Service
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
#pragma warning disable CS8600 // Преобразование литерала, допускающего значение NULL или возможного значения NULL в тип, не допускающий значение NULL.
            //NotificationCompat.Builder создаёт уведомление.
            //"UiPriorityChannel" связывает это уведомление с каналом, который вы зарегистрировали в методе CreateNotificationChannel
            Notification notification = new NotificationCompat.Builder(this, "UiPriorityChannel")
                .SetContentTitle("Повышение приоритета интерфейса")
                .SetContentText("Интерфейс сейчас активен")
                .SetSmallIcon(Resource.Mipmap.appicon_round)
                .Build();
#pragma warning restore CS8600 // Преобразование литерала, допускающего значение NULL или возможного значения NULL в тип, не допускающий значение NULL.
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.

            // Запускаем сервис как Foreground Service
            StartForeground(1, notification);

            return StartCommandResult.NotSticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            return null; // Foreground Service не требует биндинга
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}



//////////////////////////
//Общая суть

//В MainActivity создаётся и регистрируется канал уведомлений UiPriorityChannel, что гарантирует, что уведомления, которые будут созданы через этот канал, смогут быть отображены системой Android.
//Сервис UiPriorityService отвечает за создание уведомления и его отображение, а также за выполнение фоновой задачи.
//Вызов UiPriorityService происходит в MainPage, где при загрузке страницы отправляется Intent для запуска сервиса.
//Последовательность действий с точки зрения Android:
//Приложение запускается, и Android ищет MainActivity:

//Так как MainActivity имеет атрибут [Activity(MainLauncher = true)], этот класс становится "точкой входа" приложения.
//В методе OnCreate каналы уведомлений создаются с помощью метода CreateNotificationChannel.
//MainActivity вызывает CreateNotificationChannel:

//Канал UiPriorityChannel регистрируется через системный NotificationManager.
//Канал гарантирует, что уведомления, которые будут привязаны к данному каналу, смогут быть корректно отображены пользователю.
//При загрузке MainPage, вызывается запуск сервиса:

//Код в конструкторе MainPage создаёт Intent, чтобы отправить запрос на запуск UiPriorityService.
//Этот запрос обрабатывается системой Android.
//Android активирует UiPriorityService:

//Android запускает класс UiPriorityService через его метод OnStartCommand.
//Внутри OnStartCommand создаётся уведомление, привязанное к каналу UiPriorityChannel, который регистрировался ранее в MainActivity.
//Сервис запускается в режиме Foreground Service, а уведомление становится видимым в панели уведомлений.

//Создание и регистрация канала в MainActivity:
//В MainActivity создаётся канал с определённым ID (например, UiPriorityChannel), который описывает настройки уведомлений (важность, описание и т.д.).
//Канал регистрируется в операционной системе Android, чтобы уведомления, созданные через этот канал, могли правильно отображаться.
//Переход в MainPage:
//Когда приложение входит в главную страницу (MainPage), выполняется код, который создаёт Intent.
//Этот Intent сообщает системе Android: "Запусти Foreground Service (UiPriorityService)", создавая запрос на запуск.
//Android запускает UiPriorityService:
//Система Android обрабатывает запрос через Intent и запускает сервис UiPriorityService.
//Внутри UiPriorityService срабатывает метод OnStartCommand, который:
//Создаёт уведомление, связанное с каналом уведомлений (тот самый UiPriorityChannel, созданный ранее в MainActivity).
//Запускает Foreground Service, а уведомление становится видимым в панели уведомлений.