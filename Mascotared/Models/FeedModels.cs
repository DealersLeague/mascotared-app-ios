using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mascotared.Models;

// ── Feed / Publicaciones ─────────────────────────────────────────────────────
public class MomentoItem : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string UsuarioInicial { get; set; } = string.Empty;
    public string? UsuarioFotoBase64 { get; set; }

    public bool TieneFoto => !string.IsNullOrEmpty(UsuarioFotoBase64);
    public ImageSource? UsuarioFotoSource => TieneFoto
        ? ImageSource.FromStream(() =>
        {
            var bytes = Convert.FromBase64String(UsuarioFotoBase64!);
            return new MemoryStream(bytes);
        })
        : null;

    public string TiempoPublicado { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public string? ImagenBase64 { get; set; }
    public string ImagenUrl { get; set; } = string.Empty;

    public ImageSource ImagenSource => !string.IsNullOrEmpty(ImagenBase64)
        ? ImageSource.FromStream(() =>
        {
            var bytes = Convert.FromBase64String(ImagenBase64!);
            return new MemoryStream(bytes);
        })
        : ImageSource.FromUri(new Uri(ImagenUrl));

    private int _numLikes;
    public int NumLikes
    {
        get => _numLikes;
        set { _numLikes = value; OnPropertyChanged(); }
    }

    private bool _meGusta;
    public bool MeGusta
    {
        get => _meGusta;
        set { _meGusta = value; OnPropertyChanged(); }
    }

    private int _numComentarios;
    public int NumComentarios
    {
        get => _numComentarios;
        set { _numComentarios = value; OnPropertyChanged(); }
    }

    public bool EsFavorito { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// ── Mascotas destacadas ──────────────────────────────────────────────────────
public class MascotaDestacada
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoAnimal { get; set; } = string.Empty;
    public string Dueno { get; set; } = string.Empty;
    public string DuenoId { get; set; } = string.Empty;
    public string? ImagenBase64 { get; set; }
    public string ImagenUrl { get; set; } = string.Empty;

    public ImageSource ImagenSource => !string.IsNullOrEmpty(ImagenBase64)
        ? ImageSource.FromStream(() =>
        {
            var bytes = Convert.FromBase64String(ImagenBase64!);
            return new MemoryStream(bytes);
        })
        : ImageSource.FromFile(string.IsNullOrEmpty(ImagenUrl) ? "placeholder_pet.png" : ImagenUrl);
}

// ── Comentarios ──────────────────────────────────────────────────────────────
public class ComentarioItem
{
    public int Id { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string? UsuarioFotoBase64 { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string TiempoPublicado { get; set; } = string.Empty;

    public bool TieneFoto => !string.IsNullOrEmpty(UsuarioFotoBase64);
    public ImageSource? FotoSource => TieneFoto
        ? ImageSource.FromStream(() =>
        {
            var bytes = Convert.FromBase64String(UsuarioFotoBase64!);
            return new MemoryStream(bytes);
        })
        : null;
}