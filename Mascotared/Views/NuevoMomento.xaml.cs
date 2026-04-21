using Mascotared.Services;

namespace Mascotared;

public partial class NuevoMomento : ContentPage
{
    private string? _rutaImagenSeleccionada;
    private Action<MomentoItem>? _onPublicado;
    private readonly ApiService _api = new();
    private bool _publicando = false;

    public NuevoMomento(Action<MomentoItem> onPublicado)
    {
        InitializeComponent();
        _onPublicado = onPublicado;
        LblUsuario.Text = UsuarioService.Instancia.Usuario.Nombre;
        LblInicial.Text = UsuarioService.Instancia.Usuario.Inicial;
    }

    private async void OnSeleccionarFotoTapped(object sender, TappedEventArgs e)
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.Photos>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlertAsync("Permiso denegado",
                    "Necesitamos acceso a tu galería para subir fotos.", "OK");
                return;
            }

            var resultado = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Selecciona una foto"
            });

            if (resultado != null)
            {
                var (dataUri, bytes) = await FileResultToDataUriAsync(resultado);
                _rutaImagenSeleccionada = dataUri;
                ImagenPreview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                FramePlaceholder.IsVisible = false;
                ImagenPreview.IsVisible = true;
                BtnCambiarFoto.IsVisible = true;
            }
        }
        catch
        {
            await DisplayAlertAsync("Error", "No se pudo acceder a la galería.", "OK");
        }
    }

    private async void OnCambiarFotoTapped(object sender, TappedEventArgs e)
        => await SeleccionarFoto();

    private async Task SeleccionarFoto()
    {
        try
        {
            var resultado = await MediaPicker.PickPhotoAsync();
            if (resultado != null)
            {
                var (dataUri, bytes) = await FileResultToDataUriAsync(resultado);
                _rutaImagenSeleccionada = dataUri;
                ImagenPreview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
        }
        catch
        {
            await DisplayAlertAsync("Error", "No se pudo acceder a la galería.", "OK");
        }
    }

    private async void OnPublicarClicked(object sender, EventArgs e)
    {
        if (_publicando) return;
        _publicando = true;

        if (_rutaImagenSeleccionada == null)
        {
            await DisplayAlertAsync("Falta la foto", "Selecciona una foto antes de publicar.", "OK");
            _publicando = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(EntryDescripcion.Text))
        {
            await DisplayAlertAsync("Falta la descripción", "Escribe algo antes de publicar.", "OK");
            _publicando = false;
            return;
        }

        try
        {
            var id = await _api.CrearPublicacionAsync(
                _rutaImagenSeleccionada,
                EntryDescripcion.Text.Trim());

            if (id == 0)
            {
                await DisplayAlertAsync("Error", "No se pudo publicar. Inténtalo de nuevo.", "OK");
                _publicando = false;
                return;
            }

            var nuevo = new MomentoItem
            {
                Id = id,
                UsuarioNombre = UsuarioService.Instancia.Usuario.Nombre,
                UsuarioInicial = UsuarioService.Instancia.Usuario.Inicial,
                TiempoPublicado = "Ahora mismo",
                Descripcion = EntryDescripcion.Text.Trim(),
                ImagenUrl = _rutaImagenSeleccionada,
                NumLikes = 0,
                NumComentarios = 0,
                MeGusta = false,
                EsFavorito = false
            };

            _onPublicado?.Invoke(nuevo);  // ✅ Esto marca _recienPublicado = true en Momentos
            _publicando = false;          // ✅ Reset aunque la página se cierre
            await Navigation.PopAsync(animated: true);
        }
        catch
        {
            await DisplayAlertAsync("Error", "No se pudo publicar. Inténtalo de nuevo.", "OK");
            _publicando = false;
        }
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
        => await Navigation.PopAsync(animated: true);

    private static async Task<(string dataUri, byte[] bytes)> FileResultToDataUriAsync(FileResult file)
    {
        await using var stream = await file.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var mime = ext switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        var dataUri = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
        return (dataUri, bytes);
    }
}