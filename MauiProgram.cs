using glove_e.Services;
using glove_e.ViewModels;
using glove_e.Views;
using Microsoft.Extensions.Logging;

namespace glove_e
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

            // ---------- Inyección de dependencias (MVVM) ----------

            // Services (Model layer) — singletons: una sola instancia para toda la app
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IBleService, BleService>();
            builder.Services.AddSingleton<IAlertService, AlertService>();

            // ViewModels
            builder.Services.AddSingleton<MainViewModel>();   // singleton: mantiene la conexión BLE viva
            builder.Services.AddTransient<HistoryViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<OnboardingViewModel>();

            // Views
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<HistoryPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<OnboardingPage>();

            // Shell
            builder.Services.AddSingleton<AppShell>();

            return builder.Build();
        }
    }
}
