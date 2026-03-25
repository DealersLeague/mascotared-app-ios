using Mascotared.Models;
namespace Mascotared;

public class UsuarioService
{
    private static UsuarioService? _instancia;
    public static UsuarioService Instancia => _instancia ??= new UsuarioService();

    public UsuarioItem Usuario { get; private set; } = new UsuarioItem
    {
        Nombre = string.Empty,
        Edad = 0,
        Localizacion = string.Empty,
        Verificado = false,
        Tags = new List<string>(),
        TarifaPorHora = 0,
        DescripcionPersonal = string.Empty,
        DiasDisponibles = string.Empty,
        FranjasHorarias = string.Empty,
        Idioma = "Español",
        Tema = "Claro",
        TamanoLetra = "Mediano",
        Mascotas = new List<MascotaItem>()
    };

    public void ActualizarUsuario(UsuarioItem nuevo) => Usuario = nuevo;

    // Llamar al cerrar sesión — limpia todos los datos en memoria
    public void Reset() => Usuario = new UsuarioItem
    {
        Nombre = string.Empty,
        Edad = 0,
        Localizacion = string.Empty,
        Verificado = false,
        Tags = new List<string>(),
        TarifaPorHora = 0,
        DescripcionPersonal = string.Empty,
        DiasDisponibles = string.Empty,
        FranjasHorarias = string.Empty,
        Idioma = "Español",
        Tema = "Claro",
        TamanoLetra = "Mediano",
        Mascotas = new List<MascotaItem>()
    };
}