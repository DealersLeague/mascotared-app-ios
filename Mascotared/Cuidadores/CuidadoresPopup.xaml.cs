namespace Mascotared;

public partial class CuidadoresPopup : ContentPage
{
    private readonly CuidadorItem _cuidador;
    private readonly Mascotared.Services.ApiService _api = new();
    private bool _esFavorito = false;
    private bool _cerrando = false;

    public CuidadoresPopup(CuidadorItem cuidador)
    {
        InitializeComponent();
        _cuidador = cuidador;
        RellenarDatos();
        _ = CargarEstadoFavoritoAsync();
    }

    private void RellenarDatos()
    {
        var c = _cuidador;

        LblInicial.Text = c.Inicial;

        // Foto de perfil (URL o base64)
        if (!string.IsNullOrEmpty(c.FotoPerfil))
        {
            try
            {
                ImageSource src;
                if (c.FotoPerfil.StartsWith("http://") || c.FotoPerfil.StartsWith("https://"))
                    src = ImageSource.FromUri(new Uri(c.FotoPerfil));
                else
                {
                    string b64 = c.FotoPerfil.Contains(',')
                        ? c.FotoPerfil[(c.FotoPerfil.IndexOf(',') + 1)..] : c.FotoPerfil;
                    b64 = b64.Trim().Replace("\n", "").Replace("\r", "");
                    var bytes = Convert.FromBase64String(b64);
                    src = ImageSource.FromStream(() => new MemoryStream(bytes));
                }
                ImgFoto.Source = src;
                ImgFotoBorder.IsVisible = true;
            }
            catch { }
        }

        LblLikesCount.Text = c.NumeroLikes.ToString();
        LblNombre.Text = c.Nombre;
        LblLocalizacion.Text = c.Localizacion;
        LblEdad.Text = $"{c.Edad} años";
        LblTarifa.Text = $"{c.TarifaPorHora:F2} €/h";
        LblZona.Text = c.Localizacion;
        LblReservas.Text = $"{c.NumeroReservas} completadas";
        LblDescripcion.Text = c.DescripcionPersonal;
        LblTag.Text = c.Tag;
        BtnContactar.Text = $"Contactar a {c.Nombre}";
        BadgeVerificado.IsVisible = c.Verificado;
        LblTieneMascotas.Text = c.TieneMascotas ? "Sí" : "No";
        LblNumMascotas.Text = c.NumeroMascotasACuidar.ToString();
        LblPuedeNecesidades.Text = c.PuedeCuidarNecesidadesEspeciales ? "Sí" : "No";

        // ── Valoración ────────────────────────────────────────────────────────
        // Las reseñas vienen de la API a través de CuidadorItem.
        // Si NumeroResenas == 0 el usuario aún no tiene ninguna → mostramos "—"
        // y el texto "Sin valoraciones aún" en lugar de un número inventado.
        if (c.NumeroResenas == 0)
        {
            LblValoracion.Text = "—";
            LblResenas.Text = "Sin valoraciones aún";
        }
        else
        {
            LblValoracion.Text = c.Valoracion.ToString("F1");
            LblResenas.Text = $"Basado en {c.NumeroResenas} reseña{(c.NumeroResenas == 1 ? "" : "s")}";
        }

        // ── Experiencia en necesidades especiales ─────────────────────────────
        FrameExperiencia.IsVisible = c.HaCuidadoNecesidadesEspeciales;
        if (c.HaCuidadoNecesidadesEspeciales)
            LblExperiencia.Text = "Con experiencia en necesidades especiales";

        // ── Chips ─────────────────────────────────────────────────────────────
        foreach (var dia in c.ListaDias)
            FlexDias.Children.Add(CrearChip(dia, "#455AEB", "White"));

        foreach (var franja in c.ListaFranjas)
            FlexFranjas.Children.Add(CrearChip(franja, "White", "#455AEB", borde: "#455AEB"));

        foreach (var animal in c.TiposAnimalACuidar)
            FlexAnimales.Children.Add(CrearChip(animal, "#F4F6FA", "#455AEB"));
    }

    private static Border CrearChip(string texto, string fondo, string textoColor,
                                    string? borde = null)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb(fondo),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            { CornerRadius = new CornerRadius(16) },
            Stroke = borde is not null ? Color.FromArgb(borde) : Colors.Transparent,
            StrokeThickness = borde is not null ? 1 : 0,
            Padding = new Thickness(12, 6),
            Margin = new Thickness(0, 0, 6, 6),
            Content = new Label
            {
                Text = texto,
                TextColor = Color.FromArgb(textoColor),
                FontFamily = "OpenSansSemibold",
                FontSize = 12
            }
        };
    }

    private async Task CargarEstadoFavoritoAsync()
    {
        try
        {
            var favs = await _api.GetFavoritosAsync();
            _esFavorito = favs?.Any(f => f.CuidadorId == _cuidador.UsuarioId) ?? false;
            LblHeart.Text = _esFavorito ? "❤️" : "🤍";
        }
        catch { }
    }

    private async void OnHeartTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (_esFavorito)
            {
                await _api.EliminarFavoritoAsync(_cuidador.UsuarioId);
                _esFavorito = false;
                LblHeart.Text = "🤍";
                int n = int.TryParse(LblLikesCount.Text, out var v) ? v : 0;
                LblLikesCount.Text = Math.Max(0, n - 1).ToString();
            }
            else
            {
                await _api.AgregarFavoritoAsync(_cuidador.UsuarioId);
                _esFavorito = true;
                LblHeart.Text = "❤️";
                int n = int.TryParse(LblLikesCount.Text, out var v) ? v : 0;
                LblLikesCount.Text = (n + 1).ToString();
            }
        }
        catch { }
    }

    private async void OnVerPerfilTapped(object? sender, TappedEventArgs e)
    {
        var perfilPage = new Mascotared.Perfil.PerfilPublico(
            _cuidador.UsuarioId, _cuidador.FotoPerfil, _cuidador.Nombre);
        await Navigation.PopModalAsync(animated: false);
        await Shell.Current.Navigation.PushAsync(perfilPage, animated: true);
    }

    private async void OnBackdropTapped(object? sender, TappedEventArgs e)
    {
        if (_cerrando) return;
        _cerrando = true;
        await Navigation.PopModalAsync(animated: true);
    }

    private async void OnCloseTapped(object? sender, TappedEventArgs e)
    {
        if (_cerrando) return;
        _cerrando = true;
        await Navigation.PopModalAsync(animated: true);
    }

    private async void OnContactarClicked(object? sender, EventArgs e)
    {
        // Enviar solicitud automáticamente a la oferta del cuidador al contactar
        if (_cuidador.Id > 0)
        {
            var api = new Mascotared.Services.ApiService();
            await api.EnviarSolicitudAsync(_cuidador.Id,
                $"Hola, me interesa tu oferta de cuidado.");
            // No bloqueamos si falla (puede que ya exista una solicitud previa)
        }

        var conversacion = new Mascotared.Models.ConversacionItem
        {
            ContactoId = _cuidador.UsuarioId,      // UserId real del cuidador
            NombreContacto = _cuidador.Nombre,
            UltimoMensaje = "",
            FechaUltimoMensaje = DateTime.Now,
            NoLeidos = 0,
            MascotaRelacionada = null,
            TituloSolicitud = $"Solicitud · {_cuidador.Nombre}",
            DetalleSolicitud = $"{_cuidador.Localizacion} · {_cuidador.TarifaPorHora:F2} €/h",
            Valoracion = _cuidador.NumeroResenas > 0
                ? $"{_cuidador.Valoracion:F1}/5"
                : "Sin valoraciones",
            TiempoRespuesta = $"Activo: {_cuidador.UltimaActividad}",
            SoyPropietario = true   // quien contacta a un cuidador ES el propietario de la oferta
        };

        await Mascotared.Services.ConversacionesRepository.Instance
            .GuardarOActualizarAsync(conversacion);

        await Navigation.PopModalAsync(animated: false);

        var rootPage = Application.Current?.Windows[0].Page;
        if (rootPage is not null)
            await rootPage.Navigation.PushAsync(new Views.ChatPage(conversacion));
    }
    private async void OnEditarClicked(object? sender, EventArgs e)
        => await DisplayAlertAsync("Editar", "Formulario de edición próximamente.", "OK");

    private async void OnEliminarClicked(object? sender, EventArgs e)
    {
        bool ok = await DisplayAlertAsync("Eliminar",
            $"¿Eliminar el perfil de {_cuidador.Nombre}?", "Eliminar", "Cancelar");
        if (ok)
            await Navigation.PopModalAsync(animated: true);
    }
}