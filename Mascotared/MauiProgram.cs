using Mascotared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Media;

namespace Mascotared
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
                    fonts.AddFont("Inter_18pt-Regular.ttf", "InterRegular");
                    fonts.AddFont("Inter_18pt-SemiBold.ttf", "InterSemiBold");
                    fonts.AddFont("Inter_18pt-Bold.ttf", "InterBold");
                    fonts.AddFont("Inter_18pt-ExtraBold.ttf", "InterExtraBold");
                    fonts.AddFont("Inter_18pt-Light.ttf", "InterLight");
                    fonts.AddFont("Inter_18pt-Thin.ttf", "InterThin");
                    fonts.AddFont("Inter_18pt-ExtraLight.ttf", "InterExtraLight");
                    fonts.AddFont("Inter_18pt-Medium.ttf", "InterMedium");
                });

            //-----------------------NOTIFICACIONES-----------------------
            builder.Services.AddSingleton<IMensajesService, MensajesServiceLocal>();
            // Cambiar una por otra según quieras probar la simulación local o la conexión a Firebase
            // builder.Services.AddSingleton<IMensajesService, MensajesServiceFirebase>();

            // Registro de MediaPicker para selección de fotos
            builder.Services.AddSingleton<IMediaPicker>(MediaPicker.Default);

            //-----------------------ONESIGNAL-----------------------
            OneSignalService.Inicializar();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}