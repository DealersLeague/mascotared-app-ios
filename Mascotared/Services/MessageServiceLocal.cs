using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Plugin.LocalNotification.iOSOption;

namespace Mascotared.Services;

public class MensajesServiceLocal : IMensajesService
{
    public event EventHandler<MensajeRecibidoEventArgs>? MensajeRecibido;

    private readonly Random _rnd = new();

    // Respuestas simuladas del otro usuario
    private readonly List<string> _respuestasAuto = new()
    {
        "¡Hola! Sí, me viene perfecto.",
        "Perfecto, quedamos así.",
        "¿Puedes a las 10 de la mañana?",
        "Genial, muchas gracias 🐾",
        "De acuerdo, te confirmo mañana.",
        "¿Tienes experiencia con perros grandes?",
        "Muy bien, te espero."
    };

    public Task InicializarAsync()
    {
        // En local no hay nada que inicializar
        // Cuando migres a Firebase, aquí registras el token
        return Task.CompletedTask;
    }

    public async Task EnviarMensajeAsync(int conversacionId, string texto)
    {
        // Simula que el otro contesta entre 1 y 4 segundos después
        await Task.Delay(_rnd.Next(1000, 4000));

        var respuesta = _respuestasAuto[_rnd.Next(_respuestasAuto.Count)];
       // var nombre = await ObtenerNombreAsync(conversacionId);

        var args = new MensajeRecibidoEventArgs
        {
            ConversacionId = conversacionId,
          //  NombreRemitente = nombre,
            Texto = respuesta,
            Fecha = DateTime.Now
        };

        // Notificamos a quien esté escuchando (ChatPage)
        MensajeRecibido?.Invoke(this, args);

        // Lanzamos notificación del sistema
      //  await MostrarNotificacionAsync(nombre, respuesta);
    }

    private async Task MostrarNotificacionAsync(string nombre, string texto)
    {
        await LocalNotificationCenter.Current.Show(new NotificationRequest
        {
            NotificationId = _rnd.Next(1000, 9999),
            Title = nombre,
            Description = texto,
            Android = new AndroidOptions
            {
                ChannelId = "general",
                Priority = AndroidPriority.High
            },
            iOS = new iOSOptions
            {
                HideForegroundAlert = false,
                PlayForegroundSound = true
            }
        });
    }

    //private async Task<string> ObtenerNombreAsync(int conversacionId)
    //{
    //    Buscamos la conversación para saber el nombre del contacto
    //   var conv = await ConversacionesRepository.Instance.GetByIdAsync(conversacionId);
    //    return conv?.NombreContacto ?? "Contacto";
    //}
}