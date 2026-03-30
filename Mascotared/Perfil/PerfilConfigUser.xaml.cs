using Mascotared.Models;
using Mascotared.Services;

namespace Mascotared;

public partial class PerfilConfigUser : ContentPage
{
    private bool _reviewsExpandidas = false;
    private readonly ApiService _api = new();

    public PerfilConfigUser()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        CargarDatosDesdeLogin();       // render rápido desde Preferences (sin red)
        await CargarDesdeApiAsync();   // actualizar con datos frescos de la API
        await CargarMascotasAsync();
        await CargarReviewsAsync();
    }

    private async Task CargarDesdeApiAsync()
    {
        try
        {
            var perfil = await _api.GetPerfilAsync();
            if (perfil == null) return;

            if (!string.IsNullOrEmpty(perfil.NombreCompleto))
                Preferences.Set("user_nombre", perfil.NombreCompleto);
            if (!string.IsNullOrEmpty(perfil.Bio))
                Preferences.Set("user_descripcion", perfil.Bio);
            if (!string.IsNullOrEmpty(perfil.Direccion))
            {
                Preferences.Set("user_localizacion", perfil.Direccion);
                LblLocalizacion.Text = perfil.Direccion;
            }
            if (perfil.TarifaPorHora.HasValue)
                Preferences.Set("user_tarifa", perfil.TarifaPorHora.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (perfil.FechaNacimiento.HasValue)
                Preferences.Set("user_fechaNacimiento", perfil.FechaNacimiento.Value.Ticks);
            Preferences.Set("user_esPropietario", perfil.EsPropietario);
            Preferences.Set("user_esCuidador", perfil.EsCuidador);
            // Sincronizar días y franjas (antes faltaban)
            if (!string.IsNullOrEmpty(perfil.DiasDisponibles))
                Preferences.Set("user_dias", perfil.DiasDisponibles);
            if (!string.IsNullOrEmpty(perfil.FranjasHorarias))
                Preferences.Set("user_franjas", perfil.FranjasHorarias);
            if (!string.IsNullOrEmpty(perfil.Idioma))
                Preferences.Set("user_idioma", perfil.Idioma);

            // Foto: la API es la fuente de verdad
            if (!string.IsNullOrEmpty(perfil.FotoPerfil))
            {
                Preferences.Set("user_foto_base64", perfil.FotoPerfil);
                Preferences.Set("user_foto", perfil.FotoPerfil);
                UsuarioService.Instancia.Usuario.FotoPerfilBase64 = perfil.FotoPerfil;
            }

            // Refrescar UI en hilo principal con los datos actualizados
            CargarDatosDesdeLogin();
        }
        catch { }
    }

    private static int CalcularEdad(DateTime fechaNacimiento)
    {
        var hoy = DateTime.Today;
        int edad = hoy.Year - fechaNacimiento.Year;
        if (fechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
        return edad;
    }

    private void CargarDatosDesdeLogin()
    {
        string nombre = Preferences.Get("user_nombre", "Usuario");
        bool esPropietario = Preferences.Get("user_esPropietario", false);
        bool esCuidador = Preferences.Get("user_esCuidador", false);
        string localizacion = Preferences.Get("user_localizacion", "Sin ubicación");
        string descripcion = Preferences.Get("user_descripcion", "");
        string tarifa = Preferences.Get("user_tarifa", "");
        string dias = Preferences.Get("user_dias", "");
        string franjas = Preferences.Get("user_franjas", "");

        LblNombre.Text = nombre;
        LblLocalizacion.Text = localizacion;
        LblDescripcion.Text = descripcion;
        LblTarifa.Text = string.IsNullOrEmpty(tarifa)
            ? (esCuidador ? "Tarifa por definir" : "")
            : $"{tarifa} €/h";
        BadgeVerificado.IsVisible = false;
        LblIdiomaValor.Text = Preferences.Get("user_idioma", "Español");
        LblTemaValor.Text = Preferences.Get("user_tema", "Claro");
        LblLetraValor.Text = Preferences.Get("user_letra", "Mediano");

        long ticks = Preferences.Get("user_fechaNacimiento", 0L);
        LblEdad.Text = ticks > 0 ? $"{CalcularEdad(new DateTime(ticks))} años" : "";

        string fotoBase64 = Preferences.Get("user_foto_base64", "");
        if (!string.IsNullOrEmpty(fotoBase64))
            UsuarioService.Instancia.Usuario.FotoPerfilBase64 = fotoBase64;
        ActualizarAvatarUI(UsuarioService.Instancia.Usuario, nombre);

        StackTags.Children.Clear();
        if (esPropietario)
            StackTags.Children.Add(CrearChip("Propietario/a", fondo: "#EEF0FB", textoColor: "#455AEB"));
        if (esCuidador)
            StackTags.Children.Add(CrearChip("Cuidador/a", fondo: "#FFEEF4", textoColor: "#FE3D7D"));

        FlexDias.Children.Clear();
        if (!string.IsNullOrEmpty(dias))
            foreach (var dia in dias.Split(',', StringSplitOptions.RemoveEmptyEntries))
                FlexDias.Children.Add(CrearChip(dia, fondo: "#455AEB", textoColor: "#FFFFFF"));

        FlexFranjas.Children.Clear();
        if (!string.IsNullOrEmpty(franjas))
            foreach (var franja in franjas.Split(',', StringSplitOptions.RemoveEmptyEntries))
                FlexFranjas.Children.Add(CrearChip(franja, fondo: "#FFFFFF", textoColor: "#455AEB", borde: "#455AEB"));
    }

    private async Task CargarMascotasAsync()
    {
        string miId = Preferences.Get("user_id", string.Empty);
        var todas = await MascotaRepository.Instance.GetAllAsync();

        var misMascotas = todas
            .Where(m => string.IsNullOrEmpty(m.UsuarioId) || m.UsuarioId == miId)
            .ToList();

        StackMascotas.Children.Clear();
        foreach (var m in misMascotas)
            StackMascotas.Children.Add(CrearFilaMascotaItem(m));
    }

    // ── Reviews ───────────────────────────────────────────────────────────────
    private void OnReviewsTapped(object? sender, TappedEventArgs e)
    {
        _reviewsExpandidas = !_reviewsExpandidas;
        PanelReviews.IsVisible = _reviewsExpandidas;
        LblChevronReviews.Text = _reviewsExpandidas ? "⌄" : "›";
    }

    private async Task CargarReviewsAsync()
    {
        var resultado = await _api.GetMisReviewsAsync();

        StackReviews.Children.Clear();
        StackEstrellaMedia.Children.Clear();
        StackDesglose.Children.Clear();

        if (resultado == null || resultado.Total == 0)
        {
            LblMediaReviews.Text = "";
            LblTotalReviews.Text = "Sin reseñas aún";
            return;
        }

        LblMediaReviews.Text = $"{resultado.Media:F1}";
        LblTotalReviews.Text = $"{resultado.Total} {(resultado.Total == 1 ? "reseña" : "reseñas")}";

        int estrellasMostrar = (int)Math.Round(resultado.Media);
        for (int i = 1; i <= 5; i++)
            StackEstrellaMedia.Children.Add(new Label
            {
                Text = i <= estrellasMostrar ? "★" : "☆",
                FontSize = 15,
                TextColor = i <= estrellasMostrar ? Color.FromArgb("#FFB300") : Color.FromArgb("#D0D5EE")
            });

        var etiquetas = new[] { "", "Muy mal", "Mal", "Correcto", "Muy bien", "Excelente" };
        for (int estrella = 5; estrella >= 1; estrella--)
        {
            int count = resultado.Reviews.Count(r => r.Puntuacion == estrella);
            double porcentaje = resultado.Total > 0 ? (double)count / resultado.Total : 0;

            var fila = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(80) },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = new GridLength(28) }
                },
                ColumnSpacing = 8
            };

            // ✅ Etiqueta con color adaptativo
            var lblEtiqueta = new Label
            {
                Text = etiquetas[estrella],
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
            };
            lblEtiqueta.SetAppThemeColor(Label.TextColorProperty,
                Color.FromArgb("#2C2C2C"), Colors.White);

            // ✅ Fondo de barra adaptativo
            var barraFondo = new Border
            {
                HeightRequest = 8,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(4) },
                Stroke = Colors.Transparent,
                VerticalOptions = LayoutOptions.Center
            };
            barraFondo.SetAppThemeColor(Border.BackgroundColorProperty,
                Color.FromArgb("#E8EAFF"), Color.FromArgb("#2E3149"));

            var barraRelleno = new Border
            {
                BackgroundColor = Color.FromArgb("#455AEB"),
                HeightRequest = 8,
                HorizontalOptions = LayoutOptions.Start,
                WidthRequest = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(4) },
                Stroke = Colors.Transparent
            };

            var contenedorBarra = new Grid();
            contenedorBarra.Children.Add(barraFondo);
            contenedorBarra.Children.Add(barraRelleno);
            contenedorBarra.SizeChanged += (s, e) =>
            {
                barraRelleno.WidthRequest = contenedorBarra.Width * porcentaje;
            };

            // ✅ Contador con color adaptativo
            var lblCount = new Label
            {
                Text = count.ToString(),
                FontSize = 12,
                FontFamily = "OpenSansSemibold",
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            };
            lblCount.SetAppThemeColor(Label.TextColorProperty,
                Color.FromArgb("#455AEB"), Color.FromArgb("#5C6CFF"));

            Grid.SetColumn(lblEtiqueta, 0);
            Grid.SetColumn(contenedorBarra, 1);
            Grid.SetColumn(lblCount, 2);
            fila.Children.Add(lblEtiqueta);
            fila.Children.Add(contenedorBarra);
            fila.Children.Add(lblCount);

            StackDesglose.Children.Add(fila);
        }

        foreach (var r in resultado.Reviews)
            StackReviews.Children.Add(CrearCardReview(r));
    }

    // ── Cards ─────────────────────────────────────────────────────────────────
    private static Border CrearCardReview(ReviewItem r)
    {
        var estrellas = new HorizontalStackLayout { Spacing = 2 };
        for (int i = 1; i <= 5; i++)
            estrellas.Children.Add(new Label
            {
                Text = i <= r.Puntuacion ? "★" : "☆",
                FontSize = 13,
                TextColor = i <= r.Puntuacion ? Color.FromArgb("#FFB300") : Color.FromArgb("#D0D5EE")
            });

        // ✅ Autor con color adaptativo
        var lblAutor = new Label
        {
            Text = r.AutorNombre,
            FontFamily = "OpenSansSemibold",
            FontSize = 13
        };
        lblAutor.SetAppThemeColor(Label.TextColorProperty,
            Color.FromArgb("#2C2C2C"), Colors.White);

        var lblFecha = new Label
        {
            Text = r.FechaCreacion.ToString("dd/MM/yyyy"),
            FontSize = 11,
            TextColor = Color.FromArgb("#9AA0C4"),
            VerticalOptions = LayoutOptions.Center
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        Grid.SetColumn(lblAutor, 0);
        Grid.SetColumn(lblFecha, 1);
        header.Children.Add(lblAutor);
        header.Children.Add(lblFecha);

        var contenido = new VerticalStackLayout { Spacing = 6 };
        contenido.Children.Add(header);
        contenido.Children.Add(estrellas);

        if (!string.IsNullOrEmpty(r.Comentario))
        {
            // ✅ Comentario con color adaptativo
            var lblComentario = new Label
            {
                Text = r.Comentario,
                FontSize = 13,
                LineHeight = 1.4
            };
            lblComentario.SetAppThemeColor(Label.TextColorProperty,
                Color.FromArgb("#555555"), Color.FromArgb("#A0A8C8"));
            contenido.Children.Add(lblComentario);
        }

        // ✅ Card con fondo y borde adaptativos
        var card = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
            StrokeThickness = 1,
            Padding = new Thickness(14),
            Content = contenido
        };
        card.SetAppThemeColor(Border.BackgroundColorProperty,
            Colors.White, Color.FromArgb("#252836"));
        card.SetAppThemeColor(Border.StrokeProperty,
            Color.FromArgb("#EEF0FB"), Color.FromArgb("#2E3149"));

        return card;
    }

    private static Border CrearChip(string texto, string fondo, string textoColor, string? borde = null)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb(fondo),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
            Stroke = borde != null ? Color.FromArgb(borde) : Colors.Transparent,
            StrokeThickness = borde != null ? 1 : 0,
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

    private static Border CrearFilaMascotaItem(MascotasItem m)
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

        string emoji = m.Especie?.ToLower() switch
        {
            "perro" => "🐕",
            "gato" => "🐈",
            "conejo" => "🐇",
            "hámster" => "🐹",
            "reptil" => "🦎",
            "ave" => "🦜",
            "caballo" => "🐎",
            _ => "🐾"
        };

        grid.Add(new Label { Text = emoji, FontSize = 32, VerticalOptions = LayoutOptions.Start }, 0);

        var info = new VerticalStackLayout { Spacing = 4 };
        info.Children.Add(new Label { Text = m.Name, FontFamily = "OpenSansSemibold", FontSize = 14, TextColor = Color.FromArgb("#2C2C2C") });

        var subtitulo = new List<string>();
        if (!string.IsNullOrEmpty(m.Race)) subtitulo.Add(m.Race);
        if (!string.IsNullOrEmpty(m.Especie)) subtitulo.Add(m.Especie);
        if (subtitulo.Count > 0)
            info.Children.Add(new Label { Text = string.Join(" · ", subtitulo), FontSize = 12, TextColor = Color.FromArgb("#9AA0C4") });

        var necesidades = new List<string>();
        if (!string.IsNullOrEmpty(m.TipoMedicacion) && m.TipoMedicacion != "Ninguna") necesidades.Add($"💊 {m.TipoMedicacion}");
        if (!string.IsNullOrEmpty(m.CondicionesMedicas) && m.CondicionesMedicas != "Ninguna") necesidades.Add($"🏥 {m.CondicionesMedicas}");
        if (!string.IsNullOrEmpty(m.Alergias)) necesidades.Add($"⚠️ {m.Alergias}");
        if (!string.IsNullOrEmpty(m.NivelMovilidad) && m.NivelMovilidad != "Normal") necesidades.Add($"🦽 {m.NivelMovilidad}");

        if (necesidades.Count > 0)
        {
            var pills = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Direction = Microsoft.Maui.Layouts.FlexDirection.Row };
            foreach (var n in necesidades)
                pills.Children.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#FFEEF4"),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) },
                    Stroke = Colors.Transparent,
                    Padding = new Thickness(8, 3),
                    Margin = new Thickness(0, 2, 4, 2),
                    Content = new Label { Text = n, TextColor = Color.FromArgb("#FE3D7D"), FontFamily = "OpenSansSemibold", FontSize = 11 }
                });
            info.Children.Add(pills);
        }

        if (!string.IsNullOrEmpty(m.Description))
            info.Children.Add(new Label { Text = m.Description, FontSize = 12, TextColor = Color.FromArgb("#555555") });

        grid.Add(info, 1);
        return new Border
        {
            BackgroundColor = Color.FromArgb("#F4F6FA"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            Padding = new Thickness(14),
            Content = grid
        };
    }

    // ── Avatar ────────────────────────────────────────────────────────────────
    private void ActualizarAvatarUI(UsuarioItem u, string? nombreFallback = null)
    {
        if (!string.IsNullOrEmpty(u.FotoPerfilBase64))
        {
            string foto = u.FotoPerfilBase64;

            if (foto.StartsWith("http://") || foto.StartsWith("https://"))
            {
                ImgFotoPerfil.Source = ImageSource.FromUri(new Uri(foto));
                ImgFotoPerfil.IsVisible = true;
                FrameInicial.IsVisible = false;
                return;
            }

            try
            {
                if (foto.Contains(',')) foto = foto[(foto.IndexOf(',') + 1)..];
                foto = foto.Trim().Replace("\n", "").Replace("\r", "");
                var bytes = Convert.FromBase64String(foto);
                ImgFotoPerfil.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                ImgFotoPerfil.IsVisible = true;
                FrameInicial.IsVisible = false;
                return;
            }
            catch { }
        }

        ImgFotoPerfil.IsVisible = false;
        FrameInicial.IsVisible = true;
        string nombre = nombreFallback ?? u.Nombre;
        LblInicial.Text = string.IsNullOrEmpty(nombre) ? "?" : nombre[0].ToString().ToUpper();
    }

    private async void OnCambiarFotoTapped(object? sender, EventArgs e)
    {
        try
        {
            var resultado = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images
            });
            if (resultado == null) return;

            ImgFotoPerfil.Source = ImageSource.FromFile(resultado.FullPath);
            ImgFotoPerfil.IsVisible = true;
            FrameInicial.IsVisible = false;

            var fotoUrl = await _api.SubirImagenAsync(resultado.FullPath, "perfiles");
            if (fotoUrl == null)
            {
                await DisplayAlertAsync("Aviso", "No se pudo subir la foto al servidor.", "OK");
                return;
            }

            UsuarioService.Instancia.Usuario.FotoPerfilBase64 = fotoUrl;
            Preferences.Set("user_foto_base64", fotoUrl);
            Preferences.Set("user_foto", fotoUrl);

            bool ok = await _api.ActualizarPerfilAsync(new { fotoPerfil = fotoUrl });
            if (!ok)
                await DisplayAlertAsync("Aviso", "Foto guardada pero no se pudo sincronizar con el servidor.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    // ── Eventos ───────────────────────────────────────────────────────────────
    private async void OnAnadirMascotaTapped(object? sender, TappedEventArgs e)
        => await Navigation.PushAsync(new Views.MisMascotas());

    private async void OnEditarPerfilTapped(object? sender, TappedEventArgs e)
        => await Navigation.PushAsync(new EditarPerfil(), animated: true);

    private async void OnMisFavoritosTapped(object? sender, TappedEventArgs e)
        => await DisplayAlertAsync("Mis favoritos", "Próximamente.", "OK");

    private async void OnIdiomasTapped(object? sender, TappedEventArgs e)
    {
        var opcion = await DisplayActionSheetAsync("Idioma", "Cancelar", null, "Español", "English", "Français");
        if (opcion != null && opcion != "Cancelar") { Preferences.Set("user_idioma", opcion); LblIdiomaValor.Text = opcion; }
    }

    private async void OnTemaTapped(object? sender, TappedEventArgs e)
    {
        var opcion = await DisplayActionSheetAsync("Tema", "Cancelar", null, "Claro", "Oscuro");
        if (opcion != null && opcion != "Cancelar")
        {
            Preferences.Set("user_tema", opcion);
            LblTemaValor.Text = opcion;
            Application.Current!.UserAppTheme = opcion == "Oscuro" ? AppTheme.Dark : AppTheme.Light;
        }
    }

    private async void OnLetraTapped(object? sender, TappedEventArgs e)
    {
        var opcion = await DisplayActionSheetAsync("Tamaño de letra", "Cancelar", null, "Pequeño", "Mediano", "Grande");
        if (opcion != null && opcion != "Cancelar") { Preferences.Set("user_letra", opcion); LblLetraValor.Text = opcion; }
    }

private async void OnEliminarCuentaTapped(object? sender, TappedEventArgs e)
{
    bool confirmar = await DisplayAlertAsync("Eliminar cuenta",
        "Esta acción es irreversible. Se borrarán todos tus datos, mascotas, mensajes y reseñas. ¿Estás seguro?",
        "Sí, eliminar", "Cancelar");
    if (!confirmar) return;

    bool confirmar2 = await DisplayAlertAsync("Última confirmación",
        "¿Realmente quieres eliminar tu cuenta de forma permanente?",
        "Eliminar para siempre", "Cancelar");
    if (!confirmar2) return;

    bool ok = await _api.EliminarCuentaAsync();

    if (ok)
    {
        Preferences.Clear();
        Application.Current!.Dispatcher.Dispatch(() =>
            Application.Current.MainPage = new NavigationPage(new LogIn()));
    }
    else
    {
        await DisplayAlertAsync("Error", "No se pudo eliminar la cuenta. Inténtalo de nuevo.", "OK");
    }
}

    private void OnCerrarSesionTapped(object? sender, TappedEventArgs e)
    {
        UsuarioService.Instancia.Reset();
        Preferences.Clear();
        Application.Current!.Dispatcher.Dispatch(() =>
            Application.Current.MainPage = new NavigationPage(new LogIn()));
    }

    private async void OnBuscarTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToMainPage(Navigation);

    private async void OnFavoritosTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToFavoritos(Navigation);

    private async void OnReservasTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToSolicitudes(Navigation);

    private async void OnMensajesTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToMessages(Navigation);

    private async void OnCuentaTapped(object? sender, EventArgs e) { }
}

// ── Modelos ───────────────────────────────────────────────────────────────────
public class UsuarioItem
{
    public string Nombre { get; set; } = string.Empty;
    public int Edad { get; set; }
    public string? Localizacion { get; set; }
    public bool Verificado { get; set; }
    public List<string> Tags { get; set; } = new();
    public decimal TarifaPorHora { get; set; }
    public string? DescripcionPersonal { get; set; }
    public string? DiasDisponibles { get; set; }
    public string? FranjasHorarias { get; set; }
    public string Idioma { get; set; } = "Español";
    public string Tema { get; set; } = "Claro";
    public string TamanoLetra { get; set; } = "Mediano";
    public List<MascotaItem> Mascotas { get; set; } = new();
    public string? FotoPerfilBase64 { get; set; }

    public string Inicial => string.IsNullOrEmpty(Nombre) ? "?" : Nombre[0].ToString().ToUpper();
    public List<string> ListaDias => DiasDisponibles?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();
    public List<string> ListaFranjas => FranjasHorarias?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();
}