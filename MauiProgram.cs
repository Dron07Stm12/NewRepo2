using Microsoft.Extensions.Logging;
#if ANDROID
using Scb_Electronmash.Platforms.Android;
#endif  

namespace Scb_Electronmash
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif


            // Register the Bluetooth service
#if ANDROID
            builder.Services.AddSingleton<IBluetooth_service, AndroidBluetooth>();
#endif

            return builder.Build();
        }
    }
}


/*
 
Схема работы DI-контейнера
Code
+------------------------------------------------------+
|                 DI-КОНТЕЙНЕР                         |
|                                                      |
|  "Регистрирую сервисы..."                            |
|  IBluetooth_service -> AndroidBluetooth               |
|  IMyLogger        -> MyLogger                        |
|  ...                                                  |
+------------------------------------------------------+

       |
       v

[Запрос на создание MainPage]

MainPage конструктор:
public MainPage(IBluetooth_service bluetoothService)

       |
       v

DI-контейнер ищет: "Зарегистрирован ли IBluetooth_service?"

       |
       v

Если найден:
    - Создаёт AndroidBluetooth
    - Передаёт его в конструктор MainPage

Если НЕ найден:
    - Выдаёт ошибку: Unable to resolve service for type 'IBluetooth_service'

*************************************************************************************
   Типичный жизненный цикл
Регистрируешь сервисы в DI-контейнере (например, в MauiProgram.cs):

C#
builder.Services.AddSingleton<IBluetooth_service, AndroidBluetooth>();
DI-контейнер хранит соответствие:
IBluetooth_service → AndroidBluetooth

Когда нужен MainPage, DI-контейнер:

Видит, что конструктору MainPage нужен IBluetooth_service
Находит зарегистрированную реализацию (AndroidBluetooth)
Создаёт её (или берёт уже созданную, если Singleton)
Передаёт в конструктор MainPage
Если сервис не зарегистрирован — DI-контейнер не знает, что создавать → выдаёт ошибку.

  
 */
