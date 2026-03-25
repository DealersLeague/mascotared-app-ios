using Mascotared.Models;
using Mascotared.Services;
using Mascotared.Views;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

namespace Mascotared.Perfil;

public partial class PerfilPublico : ContentPage
{
    private readonly ApiService _api = new();
    private string _usuarioId = string.Empty;
    private bool _esPerfilPropio = false;
    private string? _fotoInicial = null;

    private readonly ObservableCollection<PostPublicoItem> _posts = new();
    private readonly ObservableCollection<OfertaPublicaItem> _ofertas = new();

    // Constructor para MI perfil (desde Cuenta)
    public PerfilPublico()
    {
        InitializeComponent();
        _esPerfilPropio = true;
        GridPosts.ItemsSource = _posts;
        ListaOfertas.ItemsSource = _ofertas;
        Title = "Mi perfil";
        Shell.SetTabBarIsVisible(this, false);
    }

    // Constructor para el perfil de OTRO usuario
    // fotoInicial: foto ya disponible desde el feed, se muestra mientras carga la API
    // nombreInicial: nombre ya conocido (del feed/chat/popup), se muestra de inmediato
    public PerfilPublico(string usuarioId, string? fotoInicial = null, string? nombreInicial = null)
    {
        InitializeComponent();
        _esPerfilPropio = false;
        _usuarioId = usuarioId;
        _fotoInicial = fotoInicial;
        GridPosts.ItemsSource = _posts;
        ListaOfertas.ItemsSource = _ofertas;
        Shell.SetTabBarIsVisible(this, false);

        // Mostrar el nombre de inmediato, sin esperar a la API
        if (!string.IsNullOrEmpty(nombreInicial))
        {
            Title = nombreInicial;
            LabelNombreCompleto.Text = nombreInicial;
            LabelInicial.Text = nombreInicial[0].ToString().ToUpper();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Si es perfil propio, leer userId desde Preferences (guardado en login)
        if (_esPerfilPropio && string.IsNullOrEmpty(_usuarioId))
            _usuarioId = Preferences.Get("user_id", string.Empty);

        if (string.IsNullOrEmpty(_usuarioId))
        {
            await DisplayAlert("Sesión", "No hay sesión activa. Inicia sesión primero.", "OK");
            return;
        }

        _posts.Clear();
        _ofertas.Clear();
        StackResenas.Children.Clear();
        StackMascotas.Children.Clear();
        StackDias.Children.Clear();
        StackFranjas.Children.Clear();

        // Mostrar foto del feed inmediatamente (evita pantalla en blanco mientras carga la API)
        if (!_esPerfilPropio && !string.IsNullOrEmpty(_fotoInicial))
            MostrarFoto(_fotoInicial);

        await CargarTodoAsync();
    }

    private async Task CargarTodoAsync()
    {
        try
        {
            await Task.WhenAll(
                CargarPerfilAsync(),
                CargarPostsAsync(),
                CargarOfertasAsync(),
                CargarResenasAsync(),
                CargarMascotasAsync()
            );
        }
        catch (Exception ex) { Debug.WriteLine($"[PerfilPublico] CargarTodo: {ex.Message}"); }
    }

    // ── Avatar helper ─────────────────────────────────────────────────────────

    private void MostrarFoto(string foto)
    {
        try
        {
            ImgAvatar.Source = foto.StartsWith("http")
                ? ImageSource.FromUri(new Uri(foto))
                : ImageSource.FromStream(() =>
                {
                    var b64 = foto.StartsWith("data:") ? foto[(foto.IndexOf(',') + 1)..] : foto;
                    return new MemoryStream(Convert.FromBase64String(b64.Trim()));
                });
            ImgAvatar.IsVisible = true;
            LabelInicial.IsVisible = false;
        }
        catch { /* mantener la inicial si la foto falla */ }
    }

    // ── Perfil ────────────────────────────────────────────────────────────────

    private async Task CargarPerfilAsync()
    {
        try
        {
            JsonElement p;

            if (_esPerfilPropio)
            {
                // Endpoint autenticado que SÍ existe: GET api/Usuario/perfil
                var perfil = await _api.GetPerfilAsync();
                if (perfil == null) return;

                // Leer foto directamente del DTO antes de serializar
                string? fotoDirecta = perfil.FotoPerfil;
                if (!string.IsNullOrEmpty(fotoDirecta))
                {
                    if (fotoDirecta.StartsWith("http"))
                        ImgAvatar.Source = ImageSource.FromUri(new Uri(fotoDirecta));
                    else
                        ImgAvatar.Source = ImageSource.FromStream(() =>
                        {
                            var b64 = fotoDirecta.StartsWith("data:")
                                ? fotoDirecta[(fotoDirecta.IndexOf(',') + 1)..]
                                : fotoDirecta;
                            return new MemoryStream(Convert.FromBase64String(b64));
                        });
                    ImgAvatar.IsVisible = true;
                    LabelInicial.IsVisible = false;
                }
                else
                {
                    // Fallback a Preferences
                    string fotoPrefs = Preferences.Get("user_foto_base64", string.Empty);
                    if (string.IsNullOrEmpty(fotoPrefs))
                        fotoPrefs = Preferences.Get("user_foto", string.Empty);
                    if (!string.IsNullOrEmpty(fotoPrefs))
                    {
                        ImgAvatar.Source = fotoPrefs.StartsWith("http")
                            ? ImageSource.FromUri(new Uri(fotoPrefs))
                            : ImageSource.FromStream(() =>
                            {
                                var b64 = fotoPrefs.StartsWith("data:")
                                    ? fotoPrefs[(fotoPrefs.IndexOf(',') + 1)..]
                                    : fotoPrefs;
                                return new MemoryStream(Convert.FromBase64String(b64));
                            });
                        ImgAvatar.IsVisible = true;
                        LabelInicial.IsVisible = false;
                    }
                }

                var json = JsonSerializer.Serialize(perfil);
                p = JsonDocument.Parse(json).RootElement;
            }
            else
            {
                // Endpoint público: GET api/Usuario/perfil/{usuarioId}
                var nullable = await _api.GetPerfilPublicoAsync(_usuarioId);
                if (nullable == null) return;
                p = nullable.Value;
            }

            string nombre = p.TryGetProperty("nombreCompleto", out var n) ? n.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(nombre) && p.TryGetProperty("nombre", out var n2))
                nombre = n2.GetString() ?? "";
            if (string.IsNullOrEmpty(nombre) && p.TryGetProperty("username", out var nu))
                nombre = nu.GetString() ?? "";
            if (string.IsNullOrEmpty(nombre) && p.TryGetProperty("fullName", out var nf))
                nombre = nf.GetString() ?? "";
            // Fallback a Preferences para perfil propio
            if (string.IsNullOrEmpty(nombre) && _esPerfilPropio)
                nombre = Preferences.Get("user_nombre",
                         Preferences.Get("user_name",
                         Preferences.Get("user_email", "")));

            if (!string.IsNullOrEmpty(nombre))
            {
                Title = nombre;
                LabelNombreNav.Text = nombre;
                LabelNombreCompleto.Text = nombre;
                LabelInicial.Text = nombre[0].ToString().ToUpper();
            }

            // Foto (solo para perfil ajeno — el propio ya se cargó arriba)
            // Usamos _fotoInicial (viene del feed, garantizamos que es del usuario correcto).
            // NO confiamos en la foto de la API pública porque el backend puede devolver
            // el perfil del usuario autenticado en lugar del solicitado.
            if (!_esPerfilPropio && !string.IsNullOrEmpty(_fotoInicial))
                MostrarFoto(_fotoInicial);

            // Ubicación
            if (p.TryGetProperty("direccion", out var dir))
                LabelUbicacion.Text = dir.GetString() ?? "";

            // Bio
            string bio = "";
            if (p.TryGetProperty("bio", out var b)) bio = b.GetString() ?? "";
            else if (p.TryGetProperty("descripcionPersonal", out var dp)) bio = dp.GetString() ?? "";
            LabelBio.Text = bio;
            LabelBioCard.Text = bio;

            // Roles
            bool esCuidador = p.TryGetProperty("esCuidador", out var ec) && ec.GetBoolean();
            bool esPropietario = p.TryGetProperty("esPropietario", out var ep) && ep.GetBoolean();
            ChipCuidador.IsVisible = esCuidador;
            ChipPropietario.IsVisible = esPropietario || !esCuidador;
            AvatarRingBlue.IsVisible = esCuidador;

            // Tarifa
            if (esCuidador && p.TryGetProperty("tarifaPorHora", out var t)
                && t.ValueKind != JsonValueKind.Null)
            {
                decimal tarifa = t.GetDecimal();
                LabelTarifa.Text = $"⏱ {tarifa:F2} €/h";
                LabelTarifa.IsVisible = true;
                LabelTarifaCard.Text = $"{tarifa:F2} €/h";
                CardTarifa.IsVisible = true;
            }

            // Edad
            string edadStr = "—";
            if (p.TryGetProperty("edad", out var ed) && ed.ValueKind != JsonValueKind.Null
                && ed.TryGetInt32(out int edadInt))
                edadStr = $"{edadInt} años";
            else if (p.TryGetProperty("fechaNacimiento", out var fn)
                     && fn.ValueKind != JsonValueKind.Null
                     && DateTime.TryParse(fn.GetString(), out var fechaN))
            {
                int edad = DateTime.Today.Year - fechaN.Year;
                if (DateTime.Today < fechaN.AddYears(edad)) edad--;
                edadStr = $"{edad} años";
            }
            LabelEdad.Text = edadStr;
            SeccionInfo.IsVisible = true;

            // Días disponibles
            if (p.TryGetProperty("diasDisponibles", out var dd) && dd.ValueKind != JsonValueKind.Null)
            {
                string? dias = dd.GetString();
                if (!string.IsNullOrEmpty(dias))
                {
                    foreach (var dia in dias.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        StackDias.Children.Add(CrearChip(dia.Trim(), "#EEF0FB", "#455AEB"));
                    HeaderDias.IsVisible = true;
                    ScrollDias.IsVisible = true;
                }
            }

            // Franjas horarias
            if (p.TryGetProperty("franjasHorarias", out var fh) && fh.ValueKind != JsonValueKind.Null)
            {
                string? franjas = fh.GetString();
                if (!string.IsNullOrEmpty(franjas))
                {
                    foreach (var f in franjas.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        StackFranjas.Children.Add(CrearChip(f.Trim(), "#FFF0F5", "#FE3D7D"));
                    HeaderFranjas.IsVisible = true;
                    ScrollFranjas.IsVisible = true;
                }
            }
        }
        catch (Exception ex) { Debug.WriteLine($"[PerfilPublico] CargarPerfil: {ex.Message}"); }
    }

    // ── Posts ─────────────────────────────────────────────────────────────────

    private async Task CargarPostsAsync()
    {
        try
        {
            var lista = await _api.GetPublicacionesDeUsuarioAsync(_usuarioId);
            _posts.Clear();
            foreach (var item in lista)
            {
                int id = item.TryGetProperty("id", out var pid) ? pid.GetInt32() : 0;
                string desc = item.TryGetProperty("descripcion", out var d) ? d.GetString() ?? "" : "";
                int likes = item.TryGetProperty("numLikes", out var nl) ? nl.GetInt32() : 0;

                // ── FIX: la API devuelve "imagen", no "imagenBase64" ni "imagenUrl" ──
                string imagenUrl = string.Empty;
                if (item.TryGetProperty("imagen", out var img) && img.ValueKind != JsonValueKind.Null)
                {
                    string? imgStr = img.GetString();
                    if (!string.IsNullOrEmpty(imgStr))
                        imagenUrl = imgStr.StartsWith("http") ? imgStr
                            : imgStr.StartsWith("data:") ? imgStr
                            : $"data:image/jpeg;base64,{imgStr}";
                }

                int comentarios = item.TryGetProperty("numComentarios", out var nc) ? nc.GetInt32() : 0;
                bool meGusta = item.TryGetProperty("meGusta", out var mg) && mg.GetBoolean();

                _posts.Add(new PostPublicoItem
                {
                    Id = id,
                    Descripcion = desc,
                    NumLikes = likes,
                    NumComentarios = comentarios,
                    MeGusta = meGusta,
                    ImagenUrl = imagenUrl,
                    TieneLikes = likes > 0,
                    LikesTexto = likes > 0 ? $"♥ {likes}" : ""
                });
            }
            LabelNumPosts.Text = _posts.Count.ToString();
            EstadoVacioPosts.IsVisible = _posts.Count == 0;
        }
        catch (Exception ex) { Debug.WriteLine($"[PerfilPublico] Posts: {ex.Message}"); }
    }

    // ── Ofertas ───────────────────────────────────────────────────────────────

    private async Task CargarOfertasAsync()
    {
        try
        {
            var lista = await _api.GetOfertasDeUsuarioAsync(_usuarioId);
            _ofertas.Clear();
            foreach (var o in lista)
            {
                bool esCuidador = o.TryGetProperty("esCuidador", out var ec) && ec.GetBoolean();
                decimal? tarifa = o.TryGetProperty("tarifaPorHora", out var th)
                    && th.ValueKind != JsonValueKind.Null ? th.GetDecimal() : null;
                decimal? total = o.TryGetProperty("precioTotal", out var pt)
                    && pt.ValueKind != JsonValueKind.Null ? pt.GetDecimal() : null;

                _ofertas.Add(new OfertaPublicaItem
                {
                    Id = o.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                    Titulo = o.TryGetProperty("titulo", out var t) ? t.GetString() ?? "" : "",
                    FotoPath = o.TryGetProperty("fotoPath", out var fp)
                        && fp.ValueKind != JsonValueKind.Null ? fp.GetString() ?? "" : "",
                    Localizacion = o.TryGetProperty("localizacion", out var loc)
                        ? loc.GetString() ?? "" : "",
                    EsCuidador = esCuidador,
                    TarifaPorHora = tarifa,
                    PrecioTotal = total,
                    PrecioTexto = tarifa.HasValue ? $"{tarifa:F2} €/h"
                        : total.HasValue ? $"{total:F2} €" : ""
                });
            }
            EstadoVacioOfertas.IsVisible = _ofertas.Count == 0;
        }
        catch (Exception ex) { Debug.WriteLine($"[PerfilPublico] Ofertas: {ex.Message}"); }
    }

    // ── Reseñas ───────────────────────────────────────────────────────────────

    private async Task CargarResenasAsync()
    {
        try
        {
            var resultado = await _api.GetReviewsDeUsuarioAsync(_usuarioId);
            if (resultado == null || resultado.Total == 0) return;

            LabelNumResenas.Text = resultado.Total.ToString();
            LabelMedia.Text = resultado.Media.ToString("F1");
            LabelEstrellaMedia.Text = resultado.Media.ToString("F1");

            foreach (var r in resultado.Reviews.Take(10))
            {
                var card = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                    BackgroundColor = Color.FromArgb("#F8F9FF"),
                    Stroke = Color.FromArgb("#EEF0FB"),
                    Padding = new Thickness(14, 12),
                    WidthRequest = 220
                };
                var stack = new VerticalStackLayout { Spacing = 8 };

                // Cabecera: avatar + nombre del autor (estilo For You feed)
                var header = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
                header.Children.Add(CrearAvatarView(r.AutorNombre, r.AutorFoto, 30));
                header.Children.Add(new Label
                {
                    Text = r.AutorNombre,
                    FontFamily = "InterSemiBold",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#2C2C2C"),
                    VerticalOptions = LayoutOptions.Center
                });
                stack.Children.Add(header);

                string estrellas = new string('★', r.Puntuacion) + new string('☆', 5 - r.Puntuacion);
                stack.Children.Add(new Label { Text = estrellas, TextColor = Color.FromArgb("#F5A623"), FontSize = 14 });
                stack.Children.Add(new Label { Text = r.Comentario, FontSize = 13, TextColor = Color.FromArgb("#4A4A4A"), LineBreakMode = LineBreakMode.WordWrap, MaxLines = 3 });
                card.Content = stack;

                // Tap → mostrar reseña completa en popup
                var tap = new TapGestureRecognizer();
                var reviewCopy = r; // captura local para el closure
                tap.Tapped += async (_, _) =>
                {
                    string fecha = reviewCopy.FechaCreacion.ToString("dd/MM/yyyy");
                    string estrellasCompletas = new string('★', reviewCopy.Puntuacion) + new string('☆', 5 - reviewCopy.Puntuacion);
                    await DisplayAlert(
                        $"{estrellasCompletas}  {reviewCopy.Puntuacion}/5",
                        $"{reviewCopy.Comentario}\n\n— {reviewCopy.AutorNombre}\n{fecha}",
                        "Cerrar");
                };
                card.GestureRecognizers.Add(tap);

                StackResenas.Children.Add(card);
            }
            SeccionResenas.IsVisible = true;
        }
        catch (Exception ex) { Debug.WriteLine($"[PerfilPublico] Resenas: {ex.Message}"); }
    }

    // ── Mascotas ──────────────────────────────────────────────────────────────

    private async Task CargarMascotasAsync()
    {
        // El endpoint de mascotas devuelve las del usuario autenticado sin importar
        // el ID solicitado (mismo bug que el perfil). Para perfiles ajenos ocultamos
        // la sección; el usuario verá sus propias mascotas solo en su perfil propio.
        if (!_esPerfilPropio)
        {
            SeccionMascotas.IsVisible = false;
            return;
        }

        try
        {
            var lista = await _api.GetMascotasPublicasDeUsuarioAsync(_usuarioId);
            if (lista.Count == 0) return;

            foreach (var m in lista)
            {
                string nombre = m.TryGetProperty("nombre", out var nm) ? nm.GetString() ?? "" : "";
                string? foto = m.TryGetProperty("foto", out var f)
                    && f.ValueKind != JsonValueKind.Null ? f.GetString() : null;
                string especie = m.TryGetProperty("especie", out var esp) ? esp.GetString() ?? "" : "";

                var card = new VerticalStackLayout { Spacing = 6, WidthRequest = 80, HorizontalOptions = LayoutOptions.Center };
                var avatarBorder = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
                    Stroke = Color.FromArgb("#EEF0FB"),
                    StrokeThickness = 2,
                    WidthRequest = 64,
                    HeightRequest = 64,
                    BackgroundColor = Color.FromArgb("#EEF0FB"),
                    HorizontalOptions = LayoutOptions.Center
                };
                avatarBorder.Content = !string.IsNullOrEmpty(foto)
                    ? new Image { Source = ImageSource.FromUri(new Uri(foto!)), Aspect = Aspect.AspectFill, WidthRequest = 64, HeightRequest = 64 }
                    : (View)new Label { Text = nombre.Length > 0 ? nombre[0].ToString().ToUpper() : "🐾", FontSize = 22, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#455AEB") };

                card.Children.Add(avatarBorder);
                card.Children.Add(new Label { Text = nombre, FontSize = 12, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#2C2C2C"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 });
                if (!string.IsNullOrEmpty(especie))
                    card.Children.Add(new Label { Text = especie, FontSize = 11, TextColor = Color.FromArgb("#9AA0C4"), HorizontalOptions = LayoutOptions.Center });

                StackMascotas.Children.Add(card);
            }
            SeccionMascotas.IsVisible = true;
        }
        catch (Exception ex) { Debug.WriteLine($"[PerfilPublico] Mascotas: {ex.Message}"); }

    }
    // NAVIGATION BAR
    private async void OnBuscarTapped(object? sender, EventArgs e)
      => await NavigationHelper.NavigateToMainPage(Navigation);

    private async void OnFavoritosTapped(object? sender, EventArgs e)
       => await NavigationHelper.NavigateToFavoritos(Navigation);

    private async void OnReservasTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToSolicitudes(Navigation);

    private async void OnMensajesTapped(object? sender, EventArgs e)
       => await NavigationHelper.NavigateToMessages(Navigation);

    private async void OnCuentaTapped(object? sender, EventArgs e) { }


    // ── Tabs ──────────────────────────────────────────────────────────────────

    private void OnTabPostsTapped(object sender, EventArgs e) => CambiarTab("posts");
    private void OnTabOfertasTapped(object sender, EventArgs e) => CambiarTab("ofertas");

    // ── Acordeón valoraciones ─────────────────────────────────────────────────

    private void OnToggleResenas(object sender, EventArgs e)
    {
        bool abrir = !ContenidoResenas.IsVisible;
        ContenidoResenas.IsVisible = abrir;
        LblChevronResenas.Text = abrir ? "▲" : "▼";
    }

    private void CambiarTab(string tab)
    {
        bool posts = tab == "posts";
        PanelPosts.IsVisible = posts;
        PanelOfertas.IsVisible = !posts;
        TabLabelPosts.TextColor = posts ? Color.FromArgb("#455AEB") : Color.FromArgb("#9AA0C4");
        TabLinePosts.Color = posts ? Color.FromArgb("#455AEB") : Colors.Transparent;
        TabLabelOfertas.TextColor = !posts ? Color.FromArgb("#455AEB") : Color.FromArgb("#9AA0C4");
        TabLineOfertas.Color = !posts ? Color.FromArgb("#455AEB") : Colors.Transparent;
    }

    // ── Visor de fotos integrado (estilo Instagram) ───────────────────────────

    private int _visorIndice = 0;

    private void OnPostSeleccionado(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView cv) cv.SelectedItem = null;
        if (e.CurrentSelection.FirstOrDefault() is not PostPublicoItem post) return;

        _visorIndice = _posts.IndexOf(post);
        if (_visorIndice < 0) _visorIndice = 0;
        AbrirVisor();
    }

    private void AbrirVisor()
    {
        // Cabecera del visor: avatar + nombre del usuario
        VisorNombre.Text = LabelNombreCompleto.Text ?? "";
        VisorInicial.Text = LabelInicial.Text ?? "";
        if (ImgAvatar.IsVisible)
        {
            VisorAvatar.Source = ImgAvatar.Source;
            VisorAvatar.IsVisible = true;
            VisorAvatarBg.IsVisible = false;
        }
        else
        {
            VisorAvatar.IsVisible = false;
            VisorAvatarBg.IsVisible = true;
        }

        // Flechas y contador solo visibles si hay más de 1 post
        bool multiPost = _posts.Count > 1;
        VisorFlechaAnterior.IsVisible = multiPost;
        VisorFlechaSiguiente.IsVisible = multiPost;
        ContadorBorder.IsVisible = multiPost;

        MostrarVisorPost(_visorIndice);
        OverlayVisor.IsVisible = true;
    }

    private void MostrarVisorPost(int indice)
    {
        if (_posts.Count == 0) return;
        var post = _posts[indice];

        // Imagen
        string url = post.ImagenUrl ?? string.Empty;
        if (!string.IsNullOrEmpty(url))
        {
            if (url.StartsWith("http"))
                VisorImagen.Source = ImageSource.FromUri(new Uri(url));
            else if (url.StartsWith("data:"))
                VisorImagen.Source = ImageSource.FromStream(() =>
                {
                    var b64 = url[(url.IndexOf(',') + 1)..];
                    return new MemoryStream(Convert.FromBase64String(b64));
                });
            else
                VisorImagen.Source = ImageSource.FromFile(url);
        }
        else
        {
            VisorImagen.Source = null;
        }

        // Likes
        VisorLikes.Text = post.NumLikes > 0 ? $"{post.NumLikes} Me gusta" : "";
        VisorLikes.IsVisible = post.NumLikes > 0;
        VisorBotonLike.Text = post.MeGusta ? "♥" : "♡";

        // Comentarios
        VisorComentarios.Text = post.NumComentarios > 0
            ? $"Ver los {post.NumComentarios} comentarios"
            : "";
        VisorComentarios.IsVisible = post.NumComentarios > 0;

        // Descripción con nombre en negrita
        if (!string.IsNullOrEmpty(post.Descripcion))
        {
            VisorDescripcion.FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = VisorNombre.Text + " ", FontAttributes = FontAttributes.Bold, TextColor = Colors.White, FontSize = 13 },
                    new Span { Text = post.Descripcion, TextColor = Color.FromArgb("#DDDDDD"), FontSize = 13 }
                }
            };
            VisorDescripcion.IsVisible = true;
        }
        else
        {
            VisorDescripcion.IsVisible = false;
        }

        // Puntos indicadores
        VisorPuntos.Children.Clear();
        if (_posts.Count > 1)
        {
            for (int i = 0; i < _posts.Count; i++)
            {
                VisorPuntos.Children.Add(new Ellipse
                {
                    WidthRequest = 6,
                    HeightRequest = 6,
                    Fill = i == indice
                        ? new SolidColorBrush(Colors.White)
                        : new SolidColorBrush(Color.FromArgb("#66FFFFFF"))
                });
            }
        }

        // Contador N/Total
        VisorContador.Text = $"{indice + 1}/{_posts.Count}";

        // Opacidad flechas según posición
        VisorFlechaAnterior.Opacity = indice > 0 ? 0.8 : 0.2;
        VisorFlechaSiguiente.Opacity = indice < _posts.Count - 1 ? 0.8 : 0.2;
    }

    private void OnCerrarVisorTapped(object sender, EventArgs e)
        => OverlayVisor.IsVisible = false;

    private void OnCerrarVisorSwipe(object sender, SwipedEventArgs e)
        => OverlayVisor.IsVisible = false;

    private async void OnVisorLikeTapped(object sender, TappedEventArgs e)
    {
        if (_visorIndice < 0 || _visorIndice >= _posts.Count) return;
        var post = _posts[_visorIndice];
        try
        {
            var (meGusta, numLikes) = await _api.ToggleLikePublicacionAsync(post.Id);
            post.MeGusta = meGusta;
            post.NumLikes = numLikes;
            post.TieneLikes = numLikes > 0;
            post.LikesTexto = numLikes > 0 ? $"♥ {numLikes}" : "";
            VisorBotonLike.Text = meGusta ? "♥" : "♡";
            VisorLikes.Text = numLikes > 0 ? $"{numLikes} Me gusta" : "";
            VisorLikes.IsVisible = numLikes > 0;
        }
        catch
        {
            // Fallback visual si la API falla
            VisorBotonLike.Text = VisorBotonLike.Text == "♡" ? "♥" : "♡";
        }
    }

    private void OnVisorAnteriorTapped(object sender, EventArgs e)
    {
        if (_visorIndice > 0)
        {
            _visorIndice--;
            MostrarVisorPost(_visorIndice);
        }
    }

    private void OnVisorSiguienteTapped(object sender, EventArgs e)
    {
        if (_visorIndice < _posts.Count - 1)
        {
            _visorIndice++;
            MostrarVisorPost(_visorIndice);
        }
    }

    private void OnSwipeVisor(object sender, SwipedEventArgs e)
    {
        if (e.Direction == SwipeDirection.Left) OnVisorSiguienteTapped(sender, e);
        else if (e.Direction == SwipeDirection.Right) OnVisorAnteriorTapped(sender, e);
        else if (e.Direction == SwipeDirection.Down) OverlayVisor.IsVisible = false;
    }

    // ── Comentarios ───────────────────────────────────────────────────────────

    private async void OnVisorComentariosTapped(object sender, EventArgs e)
    {
        if (_visorIndice < 0 || _visorIndice >= _posts.Count) return;
        var post = _posts[_visorIndice];

        StackComentariosVisor.Children.Clear();
        EntryNuevoComentario.Text = "";
        OverlayComentarios.IsVisible = true;

        try
        {
            var lista = await _api.GetComentariosAsync(post.Id);
            foreach (var c in lista)
            {
                string autor = c.TryGetProperty("usuarioNombre", out var an) ? an.GetString() ?? "" : "";
                string texto = c.TryGetProperty("contenido", out var ct) ? ct.GetString() ?? "" : "";
                string? foto = c.TryGetProperty("usuarioFoto", out var fv) && fv.ValueKind != JsonValueKind.Null
                    ? fv.GetString() : null;
                StackComentariosVisor.Children.Add(CrearFilaComentario(autor, texto, foto));
            }

            if (lista.Count == 0)
                StackComentariosVisor.Children.Add(new Label
                {
                    Text = "Sé el primero en comentar.",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#9AA0C4"),
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 10)
                });
        }
        catch { }
    }

    private async void OnPublicarComentarioTapped(object sender, EventArgs e)
    {
        if (_visorIndice < 0 || _visorIndice >= _posts.Count) return;
        var post = _posts[_visorIndice];
        string texto = EntryNuevoComentario.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(texto)) return;

        try
        {
            var resultado = await _api.ComentarPublicacionAsync(post.Id, texto);
            if (resultado != null)
            {
                EntryNuevoComentario.Text = "";
                string miNombre = Preferences.Get("user_nombre", "Yo");
                string? miFoto = Preferences.Get("user_foto", string.Empty);
                if (string.IsNullOrEmpty(miFoto)) miFoto = null;
                StackComentariosVisor.Children.Add(CrearFilaComentario(miNombre, texto, miFoto));

                // Actualizar contador en el post
                post.NumComentarios++;
                VisorComentarios.Text = $"Ver los {post.NumComentarios} comentarios";
                VisorComentarios.IsVisible = true;
            }
        }
        catch { }
    }

    private void OnCerrarComentariosTapped(object sender, EventArgs e)
        => OverlayComentarios.IsVisible = false;

    private View CrearFilaComentario(string autor, string texto, string? foto = null)
    {
        var textCol = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Fill };

        textCol.Children.Add(new Label
        {
            Text = autor,
            FontFamily = "InterSemiBold",
            FontSize = 13,
            TextColor = Color.FromArgb("#2C2C2C")
        });
        textCol.Children.Add(new Label
        {
            Text = texto,
            FontSize = 13,
            TextColor = Color.FromArgb("#4A4A4A"),
            LineBreakMode = LineBreakMode.WordWrap
        });

        // Botón "Responder" — pre-rellena el Entry con @nombre
        var responderLabel = new Label
        {
            Text = "Responder",
            FontSize = 11,
            TextColor = Color.FromArgb("#9AA0C4"),
            Margin = new Thickness(0, 2, 0, 0)
        };
        var autorCaptura = autor;
        responderLabel.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                EntryNuevoComentario.Text = $"@{autorCaptura} ";
                EntryNuevoComentario.Focus();
            })
        });
        textCol.Children.Add(responderLabel);

        return new HorizontalStackLayout
        {
            Spacing = 10,
            Children = { CrearAvatarView(autor, foto, 30), textCol }
        };
    }

    /// <summary>Crea un avatar circular: foto real si es URL http, si no inicial sobre fondo azul.</summary>
    private static View CrearAvatarView(string nombre, string? foto, int size = 36)
    {
        var inicial = nombre.Length > 0 ? nombre[0].ToString().ToUpper() : "?";
        var grid = new Grid { WidthRequest = size, HeightRequest = size };

        // Fondo con inicial (siempre presente)
        grid.Children.Add(new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
            BackgroundColor = Color.FromArgb("#E8EAFF"),
            Stroke = Colors.Transparent,
            WidthRequest = size,
            HeightRequest = size,
            Content = new Label
            {
                Text = inicial,
                FontFamily = "InterSemiBold",
                FontSize = (int)(size * 0.38),
                TextColor = Color.FromArgb("#455AEB"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        });

        // Foto encima si es URL http (igual que el feed For You)
        if (!string.IsNullOrEmpty(foto) && foto.StartsWith("http"))
        {
            grid.Children.Add(new Image
            {
                Source = ImageSource.FromUri(new Uri(foto)),
                WidthRequest = size,
                HeightRequest = size,
                Aspect = Aspect.AspectFill,
                Clip = new EllipseGeometry
                {
                    Center = new Point(size / 2.0, size / 2.0),
                    RadiusX = size / 2.0,
                    RadiusY = size / 2.0
                }
            });
        }

        return grid;
    }

    // ── Acciones ──────────────────────────────────────────────────────────────

    private async void OnMensajeTapped(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_usuarioId)) return;
        var conv = new ConversacionItem
        {
            ContactoId = _usuarioId,
            NombreContacto = LabelNombreCompleto.Text ?? "",
            FotoContacto = Preferences.Get("user_foto_" + _usuarioId, ""),
        };
        await Navigation.PushAsync(new ChatPage(conv), animated: true);
    }

    private async void OnFavoritoTapped(object sender, TappedEventArgs e)
    {
        if (string.IsNullOrEmpty(_usuarioId)) return;
        bool ok = await _api.AgregarFavoritoAsync(_usuarioId);
        if (ok) IconFavorito.Text = "❤️";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static View CrearChip(string texto, string bgHex, string textHex)
    {
        return new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            BackgroundColor = Color.FromArgb(bgHex),
            Stroke = Colors.Transparent,
            Padding = new Thickness(12, 6),
            Content = new Label { Text = texto, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(textHex) }
        };
    }
}

public class PostPublicoItem
{
    public int Id { get; set; }
    public string ImagenUrl { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int NumLikes { get; set; }
    public int NumComentarios { get; set; }
    public bool MeGusta { get; set; }
    public bool TieneLikes { get; set; }
    public string LikesTexto { get; set; } = string.Empty;
    public string ComentariosTexto => NumComentarios > 0 ? $"💬 {NumComentarios}" : "";
    public bool TieneComentarios => NumComentarios > 0;
}

public class OfertaPublicaItem
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string FotoPath { get; set; } = string.Empty;
    public string Localizacion { get; set; } = string.Empty;
    public bool EsCuidador { get; set; }
    public decimal? TarifaPorHora { get; set; }
    public decimal? PrecioTotal { get; set; }
    public string PrecioTexto { get; set; } = string.Empty;
    public string TagTexto => EsCuidador ? "🐾 Cuidador/a" : "🏠 Propietario/a";
    public bool TieneMascotas => Mascotas.Count > 0;
    public List<MascotaOfertaItem> Mascotas { get; set; } = new();
}

public class MascotaOfertaItem
{
    public string Nombre { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🐾";
}