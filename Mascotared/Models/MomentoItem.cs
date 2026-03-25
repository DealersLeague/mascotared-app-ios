namespace Mascotared;

public class MomentoItem
{
    public int Id { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string UsuarioInicial { get; set; } = string.Empty;
    public string TiempoPublicado { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public int NumLikes { get; set; }
    public int NumComentarios { get; set; }
    public bool MeGusta { get; set; }
    public bool EsFavorito { get; set; }
    public bool EsMio { get; set; }
    public string UsuarioId { get; set; } = string.Empty;

    // Foto de perfil del autor (base64 puro, data URI, o URL)
    public string? UsuarioFoto { get; set; }

    // Computed para binding XAML — convierte UsuarioFoto a ImageSource sin converter
    public ImageSource? UsuarioFotoSource
    {
        get
        {
            if (string.IsNullOrEmpty(UsuarioFoto)) return null;
            try
            {
                if (UsuarioFoto.StartsWith("http://") || UsuarioFoto.StartsWith("https://"))
                    return ImageSource.FromUri(new Uri(UsuarioFoto));
                string b64 = UsuarioFoto.Contains(',')
                    ? UsuarioFoto[(UsuarioFoto.IndexOf(',') + 1)..] : UsuarioFoto;
                b64 = b64.Trim().Replace("\n", "").Replace("\r", "");
                var bytes = Convert.FromBase64String(b64);
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch { return null; }
        }
    }

    // true cuando hay foto de perfil → controla IsVisible del Image vs círculo inicial
    public bool TieneFoto => !string.IsNullOrEmpty(UsuarioFoto);

    // true cuando el post tiene imagen → controla IsVisible en la galería de favoritos
    public bool TieneImagenPost => !string.IsNullOrEmpty(ImagenUrl);
}

public class MascotaDestacada
{
    public string Nombre { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string DuenoId { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public string TipoAnimal { get; set; } = string.Empty;
    public string Emoji => TipoAnimal.ToLower() switch
    {
        "perro" => "🐕",
        "gato" => "🐈",
        "conejo" => "🐇",
        _ => "🐾"
    };

    // Computed para binding XAML — convierte ImagenUrl a ImageSource sin converter
    public ImageSource? ImagenSource
    {
        get
        {
            if (string.IsNullOrEmpty(ImagenUrl)) return null;
            try
            {
                if (ImagenUrl.StartsWith("http://") || ImagenUrl.StartsWith("https://"))
                    return ImageSource.FromUri(new Uri(ImagenUrl));
                string b64 = ImagenUrl.Contains(',')
                    ? ImagenUrl[(ImagenUrl.IndexOf(',') + 1)..] : ImagenUrl;
                b64 = b64.Trim().Replace("\n", "").Replace("\r", "");
                var bytes = Convert.FromBase64String(b64);
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch { return null; }
        }
    }
}