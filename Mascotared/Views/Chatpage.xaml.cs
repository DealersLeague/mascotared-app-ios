using System.Collections.ObjectModel;
using Mascotared.Models;
using Mascotared.Services;

namespace Mascotared.Views;

public class MensajeItem
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public bool EsPropio { get; set; }
    public bool Leido { get; set; }
    public TipoMensaje Tipo { get; set; } = TipoMensaje.Normal;
    public bool MostrarFecha { get; set; }

    public bool EsSistema => Tipo == TipoMensaje.Sistema;
    public bool EsMensajeNormal => Tipo == TipoMensaje.Normal;
    public bool EsEnviado => EsMensajeNormal && EsPropio;
    public bool EsRecibido => EsMensajeNormal && !EsPropio;

    public string HoraFormateada => Fecha.ToLocalTime().ToString("HH:mm");

    public string FechaFormateada
    {
        get
        {
            var local = Fecha.ToLocalTime();
            if (local.Date == DateTime.Today) return "Hoy";
            if (local.Date == DateTime.Today.AddDays(-1)) return "Ayer";
            return local.ToString("dd 'de' MMMM");
        }
    }

    public string TicksLeido => Leido ? "✓✓" : "✓";
}

public enum TipoMensaje { Normal, Sistema }

public partial class ChatPage : ContentPage
{
    private readonly ObservableCollection<MensajeItem> _mensajes = new();
    private readonly ConversacionItem _conversacion;
    private readonly ApiService _api = new();
    private readonly TaskDupp tDup = new(); 
    private readonly string _miId = Preferences.Get("user_id", string.Empty);

    private IDispatcherTimer? _timer;
    private int _ultimoId = 0;

    public ChatPage(ConversacionItem conversacion)
    {
        InitializeComponent();
        _conversacion = conversacion;

        LabelNombreHeader.Text = conversacion.NombreContacto;
        LabelValoracion.Text = conversacion.Valoracion;
        LabelRespuesta.Text = conversacion.TiempoRespuesta;
        LabelTituloSolicitud.Text = conversacion.TituloSolicitud ?? "Solicitud de cuidado";
        LabelDetalleSolicitud.Text = conversacion.DetalleSolicitud ?? "";

        // ── Avatar header: foto si existe, inicial si no ──────────────────
        if (conversacion.TieneFoto && conversacion.FotoSource != null)
        {
            ImgFotoHeader.Source = conversacion.FotoSource;
            ImgFotoHeader.IsVisible = true;
            AvatarBorder.IsVisible = false;
        }
        else
        {
            ImgFotoHeader.IsVisible = false;
            AvatarBorder.IsVisible = true;
            LabelInicialHeader.Text = conversacion.Inicial;
            AvatarBorder.BackgroundColor = Color.FromArgb(conversacion.ColorAvatar);
        }

        ListaMensajes.ItemsSource = _mensajes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarMensajesAsync();
        await CargarSolicitudPendienteAsync();
        ActualizarBannerSolicitud();
        IniciarPolling();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        _timer = null;
    }

    // ── Carga solicitud pendiente de este contacto ────────────────────────

    private async Task CargarSolicitudPendienteAsync()
    {
        try
        {
            var solicitudes = await _api.GetSolicitudesRecibidasAsync();
            var solicitud = solicitudes.FirstOrDefault(s =>
                s.SolicitanteId == _conversacion.ContactoId && !s.OfertaFinalizada);

            if (solicitud != null)
            {
                _conversacion.SolicitudId = solicitud.Id;
                _conversacion.OfertaId = solicitud.OfertaId;
                _conversacion.EstadoSolicitud = solicitud.Estado;
                _conversacion.SoyPropietario = true;

                if (!string.IsNullOrEmpty(solicitud.OfertaTitulo))
                    LabelTituloSolicitud.Text = solicitud.OfertaTitulo;

                ActualizarBannerSolicitud();
            }
        }
        catch { }
    }

    // ── Carga mensajes desde API ──────────────────────────────────────────

    private async Task CargarMensajesAsync()
    {
        if (string.IsNullOrEmpty(_conversacion.ContactoId)) return;

        try
        {
            var lista = await _api.GetMensajesAsync(_conversacion.ContactoId);
            _mensajes.Clear();
            _ultimoId = 0;

            MensajeItem? anterior = null;
            foreach (var j in lista)
            {
                try
                {
                    var emisorId = j.GetProperty("emisorId").GetString() ?? "";
                    var contenido = j.GetProperty("contenido").GetString() ?? "";
                    var fechaUtc = j.GetProperty("fechaEnvio").GetDateTime();
                    var leido = j.TryGetProperty("leido", out var l) && l.GetBoolean();
                    var id = j.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;

                    var item = new MensajeItem
                    {
                        Id = id,
                        Texto = contenido,
                        Fecha = fechaUtc,
                        EsPropio = emisorId == _miId,
                        Leido = leido,
                        Tipo = TipoMensaje.Normal,
                        MostrarFecha = anterior == null ||
                                       anterior.Fecha.ToLocalTime().Date != fechaUtc.ToLocalTime().Date
                    };
                    _mensajes.Add(item);
                    anterior = item;
                    if (id > _ultimoId) _ultimoId = id;
                }
                catch { }
            }

            if (_mensajes.Count == 0)
            {
                _mensajes.Add(new MensajeItem
                {
                    Texto = "Los mensajes podrán ser moderados. Realiza todos los pagos dentro de la app para tu seguridad.",
                    Fecha = DateTime.UtcNow,
                    Tipo = TipoMensaje.Sistema,
                    MostrarFecha = false
                });
            }

            ScrollAlFinal();

            // Persistir localmente para acceso offline
            await GuardarMensajesLocalAsync();
        }
        catch
        {
            // Sin conexión — cargar desde local
            await CargarMensajesLocalAsync();
        }
    }

    // ── Polling para nuevos mensajes (cada 5 segundos) ────────────────────

    private void IniciarPolling()
    {
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(5);
        _timer.Tick += async (s, e) => await PollearNuevosMensajesAsync();
        _timer.Start();
    }

    private async Task PollearNuevosMensajesAsync()
    {
        if (string.IsNullOrEmpty(_conversacion.ContactoId)) return;

        try
        {
            var lista = await _api.GetMensajesAsync(_conversacion.ContactoId);

            bool huboNuevos = false;
            MensajeItem? anterior = _mensajes.LastOrDefault(m => m.EsMensajeNormal);

            foreach (var j in lista)
            {
                try
                {
                    var id = j.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;
                    if (id <= _ultimoId) continue;

                    var emisorId = j.GetProperty("emisorId").GetString() ?? "";
                    var contenido = j.GetProperty("contenido").GetString() ?? "";
                    var fechaUtc = j.GetProperty("fechaEnvio").GetDateTime();
                    var leido = j.TryGetProperty("leido", out var l) && l.GetBoolean();

                    var item = new MensajeItem
                    {
                        Id = id,
                        Texto = contenido,
                        Fecha = fechaUtc,
                        EsPropio = emisorId == _miId,
                        Leido = leido,
                        Tipo = TipoMensaje.Normal,
                        MostrarFecha = anterior == null ||
                                       anterior.Fecha.ToLocalTime().Date != fechaUtc.ToLocalTime().Date
                    };
                    _mensajes.Add(item);
                    anterior = item;
                    if (id > _ultimoId) _ultimoId = id;
                    huboNuevos = true;
                }
                catch { }
            }

            if (huboNuevos)
            {
                ScrollAlFinal();
                await GuardarMensajesLocalAsync();
            }
        }
        catch { }
        // Socket closed, timeout, sin conexión — se ignora silenciosamente.
        // El timer volverá a intentarlo en 5 segundos.
    }

    // ── Banner solicitud ──────────────────────────────────────────────────

    private void ActualizarBannerSolicitud()
    {
        bool mostrarAcciones = _conversacion.SoyPropietario
            && _conversacion.EstadoSolicitud == "Pendiente"
            && _conversacion.SolicitudId.HasValue;

        GridBotonesAceptar.IsVisible = mostrarAcciones;

        bool mostrarEnviar = !_conversacion.SoyPropietario
            && string.IsNullOrEmpty(_conversacion.EstadoSolicitud)
            && _conversacion.OfertaId.HasValue;

        BannerEnviarSolicitud.IsVisible = mostrarEnviar;

        bool procesada = _conversacion.EstadoSolicitud == "Aceptada"
    || _conversacion.EstadoSolicitud == "Rechazada"
    || _conversacion.EstadoSolicitud == "Finalizada";
        BorderEstadoSolicitud.IsVisible = procesada;
        LabelEstadoSolicitud.Text = _conversacion.EstadoSolicitud switch
        {
            "Aceptada" => "✅ Solicitud aceptada",
            "Rechazada" => "❌ Solicitud rechazada",
            "Finalizada" => " Trabajo finalizado",
            _ => ""
        };
        LabelEstadoSolicitud.TextColor = _conversacion.EstadoSolicitud switch
        {
            "Aceptada" => Color.FromArgb("#22AA55"),
            "Finalizada" => Color.FromArgb("#455AEB"),
            _ => Color.FromArgb("#EF4444")
        };
        
    }

    // ── Aceptar / Rechazar solicitud desde chat ───────────────────────────

    private async void OnAceptarSolicitudTapped(object sender, EventArgs e)
    {
        if (!_conversacion.OfertaId.HasValue || string.IsNullOrEmpty(_conversacion.ContactoId)) return;

        bool ok = await DisplayAlertAsync("Aceptar solicitud",
            $"¿Aceptar a {_conversacion.NombreContacto} para esta tarea?",
            "Aceptar", "Cancelar");
        if (!ok) return;

        bool exito = await _api.AceptarSolicitanteAsync(
            _conversacion.OfertaId.Value, _conversacion.ContactoId);

        if (exito)
        {
            await tDup.GetNewTaskId(_conversacion.OfertaId.Value);
            _conversacion.EstadoSolicitud = "Aceptada";
            ActualizarBannerSolicitud();
            await DisplayAlertAsync("¡Aceptado!", $"Has aceptado a {_conversacion.NombreContacto}.", "OK");
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo aceptar la solicitud.", "OK");
        }
    }

    private async void OnRechazarSolicitudTapped(object sender, EventArgs e)
    {
        if (!_conversacion.SolicitudId.HasValue) return;

        bool ok = await DisplayAlertAsync("Rechazar solicitud",
            $"¿Rechazar la solicitud de {_conversacion.NombreContacto}?",
            "Rechazar", "Cancelar");
        if (!ok) return;

        bool exito = await _api.CancelarSolicitudAsync(_conversacion.SolicitudId.Value);
        if (exito)
        {
            _conversacion.EstadoSolicitud = "Rechazada";
            ActualizarBannerSolicitud();
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo rechazar la solicitud.", "OK");
        }
    }

    // ── Enviar solicitud (cuidador → propietario) ─────────────────────────

    private async void OnEnviarSolicitudTapped(object sender, EventArgs e)
    {
        if (!_conversacion.OfertaId.HasValue) return;

        bool ok = await DisplayAlertAsync("Enviar solicitud",
            $"¿Enviar solicitud de cuidado a {_conversacion.NombreContacto}?",
            "Enviar", "Cancelar");
        if (!ok) return;

        bool exito = await _api.EnviarSolicitudAsync(_conversacion.OfertaId.Value);
        if (exito)
        {
            _conversacion.EstadoSolicitud = "Pendiente";
            ActualizarBannerSolicitud();
            await DisplayAlertAsync("Enviada", "Espera la confirmación del propietario.", "OK");
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo enviar la solicitud. Puede que ya exista una.", "OK");
        }
    }

    // ── Enviar mensaje ────────────────────────────────────────────────────

    private void OnTextoMensajeCambiado(object sender, TextChangedEventArgs e)
        => BotonEnviar.Opacity = string.IsNullOrWhiteSpace(e.NewTextValue) ? 0.4 : 1.0;

    private async void OnEnviarMensajeTapped(object sender, EventArgs e)
    {
        string texto = EntryMensaje.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(texto)) return;

        EntryMensaje.Text = "";
        BotonEnviar.Opacity = 0.4;

        await BotonEnviar.ScaleToAsync(0.85, 80);
        await BotonEnviar.ScaleToAsync(1.0, 80);

        var item = new MensajeItem
        {
            Texto = texto,
            Fecha = DateTime.UtcNow,
            EsPropio = true,
            Leido = false,
            Tipo = TipoMensaje.Normal,
            MostrarFecha = _mensajes.Count == 0 ||
                           _mensajes.Last().Fecha.ToLocalTime().Date != DateTime.Today
        };
        _mensajes.Add(item);
        ScrollAlFinal();

        bool ok = await _api.EnviarMensajeAsync(_conversacion.ContactoId, texto);
        if (!ok)
        {
            int idx = _mensajes.IndexOf(item);
            if (idx >= 0)
            {
                _mensajes[idx] = new MensajeItem
                {
                    Texto = $"⚠️ {texto}",
                    Fecha = item.Fecha,
                    EsPropio = true,
                    Leido = false,
                    Tipo = TipoMensaje.Normal,
                    MostrarFecha = item.MostrarFecha
                };
            }
        }

        // Actualizar conversación localmente (último mensaje)
        await ConversacionesRepository.Instance
            .ActualizarUltimoMensajeAsync(_conversacion.ContactoId, texto, DateTime.Now);

        // Si la conversación no existía en local aún, crearla
        await ConversacionesRepository.Instance.GuardarOActualizarAsync(new Mascotared.Models.ConversacionItem
        {
            ContactoId = _conversacion.ContactoId,
            NombreContacto = _conversacion.NombreContacto,
            UltimoMensaje = texto,
            FechaUltimoMensaje = DateTime.Now,
            FotoContacto = _conversacion.FotoContacto,
            TituloSolicitud = _conversacion.TituloSolicitud,
            Valoracion = _conversacion.Valoracion,
            TiempoRespuesta = _conversacion.TiempoRespuesta
        });
    }

    // ── Cerrar banner de estado ───────────────────────────────────────────

    private void OnDismissBannerTapped(object sender, TappedEventArgs e)
        => BorderEstadoSolicitud.IsVisible = false;

    // ── Ver solicitud ─────────────────────────────────────────────────────

    private async void OnVerSolicitudTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToSolicitudes(Navigation);

    // ── Helpers ───────────────────────────────────────────────────────────

    private void ScrollAlFinal()
    {
        if (_mensajes.Count == 0) return;
        MainThread.BeginInvokeOnMainThread(() =>
            ListaMensajes.ScrollTo(_mensajes.Last(), ScrollToPosition.End, animate: false));
    }

    // ── Ver perfil del contacto ───────────────────────────────────────────

    private async void OnVerPerfilTapped(object sender, EventArgs e)
        => await Navigation.PushAsync(
            new Mascotared.Perfil.PerfilPublico(_conversacion.ContactoId, _conversacion.FotoContacto, _conversacion.NombreContacto));

    // ── Opciones ──────────────────────────────────────────────────────────

    private async void OnOpcionesTapped(object sender, EventArgs e)
    {
        var opciones = new List<string> { "Ver perfil", "Reportar conversación", "Bloquear usuario" };

        if (_conversacion.SoyPropietario && _conversacion.EstadoSolicitud == "Aceptada")
        {
            // Tarea activa: mostrar Finalizar y opción de asignar nueva (si finaliza primero)
            opciones.Insert(0, "Finalizar trabajo");
            opciones.Insert(1, "Asignar otra tarea");
        }
        else
        {
            opciones.Insert(0, "Asignar tarea");
        }

        string accion = await DisplayActionSheetAsync(
            "Opciones", "Cancelar", null, opciones.ToArray());

        switch (accion)
        {
            case "Finalizar trabajo":
                await FinalizarTrabajoAsync();
                break;
            case "Asignar tarea":
            case "Asignar otra tarea":
                await AsignarTareaManualAsync();
                break;
            case "Ver perfil":
                await Navigation.PushAsync(
                    new Mascotared.Perfil.PerfilPublico(_conversacion.ContactoId, _conversacion.FotoContacto, _conversacion.NombreContacto));
                break;
            case "Reportar conversación":
                await DisplayAlertAsync("Reportar",
                    "Gracias por tu reporte. Lo revisaremos pronto.", "OK");
                break;
            case "Bloquear usuario":
                bool bloquear = await DisplayAlertAsync("Bloquear",
                    $"¿Seguro que quieres bloquear a {_conversacion.NombreContacto}?",
                    "Bloquear", "Cancelar");
                if (bloquear) await Navigation.PopAsync();
                break;
        }
    }

    // ── Asignar tarea manual ──────────────────────────────────────────────

    private async Task AsignarTareaManualAsync()
    {
        bool? tipoBusqueda = _conversacion.SoyPropietario ? false : true;
        var tareas = await _api.GetOfertasAsync(esCuidador: tipoBusqueda);
        var misOfertas = tareas.Where(t => t.EsMia && !t.IsFinished).ToList();

        if (misOfertas.Count == 0)
        {
            await DisplayAlertAsync("Sin ofertas", "No tienes ofertas activas para asignar.", "OK");
            return;
        }

        var opciones = misOfertas.Select(t => t.title).ToArray();
        string elegida = await DisplayActionSheetAsync(
            $"¿Qué tarea asignar a {_conversacion.NombreContacto}?",
            "Cancelar", null, opciones);

        if (string.IsNullOrEmpty(elegida) || elegida == "Cancelar") return;

        var tarea = misOfertas.First(t => t.title == elegida);

        bool ok = await _api.AceptarSolicitanteAsync(tarea.offerId, _conversacion.ContactoId);
        if (ok)
        {
            await tDup.GetNewTaskId(tarea.offerId);
            _conversacion.OfertaId = tarea.offerId;
            _conversacion.EstadoSolicitud = "Aceptada";
            _conversacion.TituloSolicitud = tarea.title;
            LabelTituloSolicitud.Text = tarea.title;
            ActualizarBannerSolicitud();
            await DisplayAlertAsync("✅ Asignado",
                $"{_conversacion.NombreContacto} ha sido asignado a '{elegida}'.", "OK");
        }
        else
        {
            await DisplayAlertAsync("Sin solicitud previa",
                $"{_conversacion.NombreContacto} debe enviar primero una solicitud a esta tarea para poder asignarla.", "OK");
        }
    }

    // ── Persistencia local de mensajes ───────────────────────────────────────

    private string MensajesFilePath => Path.Combine(
        FileSystem.AppDataDirectory, $"mensajes_{_conversacion.ContactoId}.json");

    private async Task GuardarMensajesLocalAsync()
    {
        try
        {
            var lista = _mensajes
                .Where(m => m.EsMensajeNormal)
                .Select(m => new
                {
                    m.Id,
                    m.Texto,
                    m.Fecha,
                    m.EsPropio,
                    m.Leido
                });
            var json = System.Text.Json.JsonSerializer.Serialize(lista);
            await File.WriteAllTextAsync(MensajesFilePath, json);
        }
        catch { }
    }

    private async Task CargarMensajesLocalAsync()
    {
        try
        {
            if (!File.Exists(MensajesFilePath))
            {
                // Sin archivo local → mostrar aviso igual que en el flujo online vacío
                _mensajes.Add(new MensajeItem
                {
                    Texto = "Los mensajes podrán ser moderados. Realiza todos los pagos dentro de la app para tu seguridad.",
                    Fecha = DateTime.UtcNow,
                    Tipo = TipoMensaje.Sistema,
                    MostrarFecha = false
                });
                return;
            }
            var json = await File.ReadAllTextAsync(MensajesFilePath);
            var lista = System.Text.Json.JsonSerializer.Deserialize<
                List<System.Text.Json.JsonElement>>(json);
            if (lista == null) return;

            _mensajes.Clear();
            _ultimoId = 0;
            MensajeItem? anterior = null;

            foreach (var j in lista)
            {
                var id = j.TryGetProperty("Id", out var idP) ? idP.GetInt32() : 0;
                var texto = j.TryGetProperty("Texto", out var t) ? t.GetString() ?? "" : "";
                var fecha = j.TryGetProperty("Fecha", out var f) ? f.GetDateTime() : DateTime.UtcNow;
                var esPropio = j.TryGetProperty("EsPropio", out var ep) && ep.GetBoolean();
                var leido = j.TryGetProperty("Leido", out var l) && l.GetBoolean();

                var item = new MensajeItem
                {
                    Id = id,
                    Texto = texto,
                    Fecha = fecha,
                    EsPropio = esPropio,
                    Leido = leido,
                    Tipo = TipoMensaje.Normal,
                    MostrarFecha = anterior == null ||
                                   anterior.Fecha.ToLocalTime().Date != fecha.ToLocalTime().Date
                };
                _mensajes.Add(item);
                anterior = item;
                if (id > _ultimoId) _ultimoId = id;
            }

            if (_mensajes.Count == 0)
                _mensajes.Add(new MensajeItem
                {
                    Texto = "Los mensajes podrán ser moderados. Realiza todos los pagos dentro de la app para tu seguridad.",
                    Fecha = DateTime.UtcNow,
                    Tipo = TipoMensaje.Sistema,
                    MostrarFecha = false
                });

            ScrollAlFinal();
        }
        catch { }
    }
        // ── Finalizar trabajo (solo propietario, solo si está Aceptada) ───────
    private async Task FinalizarTrabajoAsync()
        {
        if (!_conversacion.OfertaId.HasValue) return;

        bool ok = await DisplayAlertAsync("Finalizar trabajo",
            $"¿Confirmas que el trabajo con {_conversacion.NombreContacto} ha terminado?",
            "Finalizar", "Cancelar");
        if (!ok) return;

        bool exito = await _api.FinalizarOfertaAsync(_conversacion.OfertaId.Value);
        if (exito)
        {
            _conversacion.EstadoSolicitud = "Finalizada";
            ActualizarBannerSolicitud();

            bool resenar = await DisplayAlertAsync("✅ Trabajo finalizado",
                $"¿Quieres dejar una reseña a {_conversacion.NombreContacto}?",
                "Sí", "Ahora no");

            if (resenar)
            {
                string? puntuacionStr = await DisplayActionSheetAsync(
                    $"¿Cuántas estrellas le das a {_conversacion.NombreContacto}?",
                    "Cancelar", null,
                    "⭐ 1", "⭐⭐ 2", "⭐⭐⭐ 3", "⭐⭐⭐⭐ 4", "⭐⭐⭐⭐⭐ 5");

                if (string.IsNullOrEmpty(puntuacionStr) || puntuacionStr == "Cancelar") return;
                int puntuacion = puntuacionStr.Count(c => c == '⭐');

                string? comentario = await DisplayPromptAsync(
                    "Escribe tu reseña",
                    $"Cuéntanos cómo fue tu experiencia con {_conversacion.NombreContacto}",
                    "Publicar", "Cancelar",
                    placeholder: "Escribe aquí tu comentario...",
                    maxLength: 300);

                if (string.IsNullOrEmpty(comentario)) return;

                bool okReview = await _api.CrearReviewAsync(
                    _conversacion.ContactoId,
                    puntuacion,
                    comentario,
                    _conversacion.OfertaId);

                if (okReview)
                    await DisplayAlertAsync("¡Gracias!", "Tu reseña ha sido publicada.", "OK");
                else
                    await DisplayAlertAsync("Error", "No se pudo publicar la reseña.", "OK");
            }
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo finalizar el trabajo.", "OK");
        }
    }
    // ── Volver ────────────────────────────────────────────────────────────

    private async void OnVolverTapped(object sender, EventArgs e)
        => await Navigation.PopAsync();
}