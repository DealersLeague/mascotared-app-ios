namespace Mascotared;

public partial class PropietarioPopup : ContentPage
{
    private readonly PropietarioItem _propietario;
    private readonly Mascotared.Services.ApiService _api = new();
    private bool _esFavorito = false;

    public PropietarioPopup(PropietarioItem propietario)
    {
        InitializeComponent();
        _propietario = propietario;
        RellenarDatos();
        _ = CargarEstadoFavoritoAsync();
    }

    private void RellenarDatos()
    {
        var p = _propietario;

        LblInicial.Text = p.Inicial;

        // Foto de perfil (URL o base64)
        if (!string.IsNullOrEmpty(p.FotoPerfil))
        {
            try
            {
                ImageSource src;
                if (p.FotoPerfil.StartsWith("http://") || p.FotoPerfil.StartsWith("https://"))
                    src = ImageSource.FromUri(new Uri(p.FotoPerfil));
                else
                {
                    string b64 = p.FotoPerfil.Contains(',')
                        ? p.FotoPerfil[(p.FotoPerfil.IndexOf(',') + 1)..] : p.FotoPerfil;
                    b64 = b64.Trim().Replace("\n", "").Replace("\r", "");
                    var bytes = Convert.FromBase64String(b64);
                    src = ImageSource.FromStream(() => new MemoryStream(bytes));
                }
                ImgFoto.Source = src;
                ImgFotoBorder.IsVisible = true;
            }
            catch { }
        }

        LblLikesCount.Text = p.NumeroLikes.ToString();
        LblNombre.Text = p.Nombre;
        LblLocalizacion.Text = p.Localizacion;
        LblEdad.Text = $"{p.Edad} años";
        LblTarifa.Text = $"{p.TarifaMaxPorHora:F2} €";
        LblZona.Text = p.Localizacion;
        LblReservas.Text = $"{p.NumeroReservas} completadas";
        LblDescripcion.Text = p.DescripcionPersonal;
        LblMascotasTitulo.Text = $"MASCOTAS ({p.Mascotas.Count})";
        BtnContactar.Text = $"Contactar a {p.Nombre}";
        BadgeVerificado.IsVisible = p.Verificado;

        if (p.NumeroResenas == 0)
        {
            LblValoracion.Text = "—";
            LblResenas.Text = "Sin valoraciones aún";
        }
        else
        {
            LblValoracion.Text = p.Valoracion.ToString("F1");
            LblResenas.Text = $"Basado en {p.NumeroResenas} reseña{(p.NumeroResenas == 1 ? "" : "s")}";
        }

        foreach (var dia in p.ListaDias)
            FlexDias.Children.Add(CrearChip(dia, "#455AEB", "White"));

        foreach (var franja in p.ListaFranjas)
            FlexFranjas.Children.Add(CrearChip(franja, "#FE3D7D", "White"));

        foreach (var m in p.Mascotas)
            StackMascotas.Children.Add(CrearFilaMascota(m));
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

    private static Border CrearFilaMascota(MascotaItem m)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };

        grid.Add(new Label { Text = m.Emoji, FontSize = 32, VerticalOptions = LayoutOptions.Start }, 0);

        var info = new VerticalStackLayout { Spacing = 3 };
        info.Children.Add(new Label
        {
            Text = m.Nombre,
            FontFamily = "OpenSansSemibold",
            FontSize = 14,
            TextColor = Color.FromArgb("#2C2C2C")
        });
        if (!string.IsNullOrEmpty(m.Raza))
            info.Children.Add(new Label { Text = m.Raza, FontSize = 12, TextColor = Color.FromArgb("#9AA0C4") });
        if (m.TieneCuidados)
            info.Children.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#FFEEF4"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) },
                Stroke = Colors.Transparent,
                Padding = new Thickness(8, 3),
                HorizontalOptions = LayoutOptions.Start,
                Content = new Label { Text = $"⚠️ {m.CuidadosEspeciales}", TextColor = Color.FromArgb("#FE3D7D"), FontFamily = "OpenSansSemibold", FontSize = 11 }
            });
        if (!string.IsNullOrEmpty(m.Descripcion))
            info.Children.Add(new Label { Text = m.Descripcion, FontSize = 12, TextColor = Color.FromArgb("#555555") });

        grid.Add(info, 1);

        return new Border
        {
            BackgroundColor = Color.FromArgb("#F4F6FA"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
            Stroke = Colors.Transparent,
            Padding = new Thickness(14),
            Content = grid
        };
    }

    private async Task CargarEstadoFavoritoAsync()
    {
        try
        {
            var favs = await _api.GetFavoritosAsync();
            _esFavorito = favs?.Any(f => f.CuidadorId == _propietario.UsuarioId) ?? false;
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
                await _api.EliminarFavoritoAsync(_propietario.UsuarioId);
                _esFavorito = false;
                LblHeart.Text = "🤍";
                int n = int.TryParse(LblLikesCount.Text, out var v) ? v : 0;
                LblLikesCount.Text = Math.Max(0, n - 1).ToString();
            }
            else
            {
                await _api.AgregarFavoritoAsync(_propietario.UsuarioId);
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
        await Navigation.PopModalAsync(animated: false);
        var rootPage = Application.Current?.Windows[0].Page;
        if (rootPage is not null)
            await rootPage.Navigation.PushAsync(
                new Mascotared.Perfil.PerfilPublico(_propietario.UsuarioId, _propietario.FotoPerfil, _propietario.Nombre));
    }

    private async void OnBackdropTapped(object? sender, TappedEventArgs e)
        => await Navigation.PopModalAsync(animated: true);

    private async void OnCloseTapped(object? sender, TappedEventArgs e)
        => await Navigation.PopModalAsync(animated: true);

    private async void OnContactarClicked(object? sender, EventArgs e)
    {
        // Enviar solicitud automáticamente al contactar
        if (_propietario.Id > 0)
        {
            var api = new Mascotared.Services.ApiService();
            await api.EnviarSolicitudAsync(_propietario.Id,
                $"Hola, me gustaría cuidar a tus mascotas.");
            // No bloqueamos si falla (puede que ya exista una solicitud previa)
        }

        var conversacion = new Mascotared.Models.ConversacionItem
        {
            ContactoId = _propietario.UsuarioId,   // UserId real del propietario
            NombreContacto = _propietario.Nombre,
            UltimoMensaje = "",
            FechaUltimoMensaje = DateTime.Now,
            NoLeidos = 0,
            MascotaRelacionada = _propietario.Mascotas.Count > 0
                ? $"{_propietario.Mascotas[0].Emoji} {_propietario.Mascotas[0].Nombre}"
                : null,
            TituloSolicitud = $"Solicitud · {_propietario.Nombre}",
            DetalleSolicitud = $"{_propietario.Localizacion} · {_propietario.TarifaMaxPorHora:F2} €",
            Valoracion = _propietario.NumeroResenas > 0
                ? $"{_propietario.Valoracion:F1}/5"
                : "Sin valoraciones",
            TiempoRespuesta = $"Activo: {_propietario.UltimaActividad}",
            SoyPropietario = false
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
            $"¿Eliminar el perfil de {_propietario.Nombre}?", "Eliminar", "Cancelar");
        if (ok)
            await Navigation.PopModalAsync(animated: true);
    }
}