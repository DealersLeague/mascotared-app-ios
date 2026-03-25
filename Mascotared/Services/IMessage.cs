namespace Mascotared.Services;

public interface IMensajesService
{
    // Enviar un mensaje
    Task EnviarMensajeAsync(int conversacionId, string texto);

    // Suscribirse a mensajes entrantes
    event EventHandler<MensajeRecibidoEventArgs> MensajeRecibido;

    // Arrancar el servicio
    Task InicializarAsync();
}

public class MensajeRecibidoEventArgs : EventArgs
{
    public int ConversacionId { get; set; }
    public string NombreRemitente { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Now;
}