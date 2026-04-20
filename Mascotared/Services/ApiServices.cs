using Mascotared.Models;
using Microsoft.Maui.Storage;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mascotared.Services
{
    public class LoginResult
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EsPropietario { get; set; }
        public bool EsCuidador { get; set; }
        public string? FotoPerfil { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Genero { get; set; }
        public bool EsNuevo { get; set; }
    }

    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.mascotared.es/api";

        // private const string BaseUrl = "https://10.0.2.2:7083/api";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            // quitar esta línea:
            // _httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
        }

        private void SetToken()
        {
            var token = Preferences.Get("jwt_token", "");
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }

        // ── IMÁGENES ─────────────────────────────────────────────────────────

        public async Task<string?> SubirImagenAsync(string rutaLocalOBase64, string carpeta)
        {
            try
            {
                SetToken();

                string base64;

                if (File.Exists(rutaLocalOBase64))
                {
                    var bytes = await File.ReadAllBytesAsync(rutaLocalOBase64);
                    var ext = Path.GetExtension(rutaLocalOBase64).ToLower().TrimStart('.');
                    var mime = ext switch
                    {
                        "png" => "image/png",
                        "gif" => "image/gif",
                        "webp" => "image/webp",
                        _ => "image/jpeg"
                    };
                    base64 = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                }
                else
                {
                    base64 = rutaLocalOBase64.StartsWith("data:")
                        ? rutaLocalOBase64
                        : $"data:image/jpeg;base64,{rutaLocalOBase64}";
                }

                var body = JsonSerializer.Serialize(new { base64, carpeta });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/Imagenes/subir", content);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
		var root = JsonDocument.Parse(json).RootElement;
		var url = root.GetProperty("url").GetString();

		
		return url;
            }
            catch { return null; }
        }

        // ── AUTH ─────────────────────────────────────────────────────────────

        public async Task<LoginResult?> LoginAsync(string email, string password)
        {
            var body = JsonSerializer.Serialize(new { email, password });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/Auth/login", content);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var result = new LoginResult
            {
                Token = root.GetProperty("token").GetString() ?? "",
                UserId = root.GetProperty("userId").GetString() ?? "",
                NombreCompleto = root.GetProperty("nombreCompleto").GetString() ?? "",
                Email = root.GetProperty("email").GetString() ?? "",
                EsPropietario = root.GetProperty("esPropietario").GetBoolean(),
                EsCuidador = root.GetProperty("esCuidador").GetBoolean(),
                FotoPerfil = root.TryGetProperty("fotoPerfil", out var fp) ? fp.GetString() : null,
                FechaNacimiento = root.TryGetProperty("fechaNacimiento", out var fn) && fn.ValueKind != JsonValueKind.Null
                    ? fn.GetDateTime() : null,
                Genero = root.TryGetProperty("genero", out var g) ? g.GetString() : null
            };

            Preferences.Set("jwt_token", result.Token);
            Preferences.Set("user_id", result.UserId);
            Preferences.Set("user_email", result.Email);
            if (!string.IsNullOrEmpty(result.FotoPerfil))
                Preferences.Set("user_foto", result.FotoPerfil);

            return result;
        }

        public async Task<bool> RegisterAsync(string nombre, string email, string password,
            bool esPropietario, bool esCuidador,
            DateTime? fechaNacimiento = null, string? genero = null)
        {
            var body = JsonSerializer.Serialize(new
            {
                nombreCompleto = nombre,
                email,
                password,
                esPropietario,
                esCuidador,
                fechaNacimiento,
                genero
            });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/Auth/register", content);
            return response.IsSuccessStatusCode;
        }

        // ── PERFIL ───────────────────────────────────────────────────────────

        public async Task<PerfilDto?> GetPerfilAsync()
        {
            try
            {
                SetToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/Usuario/perfil");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PerfilDto>(json, _jsonOptions);
            }
            catch { return null; }
        }

        /// <summary>
        /// Devuelve el perfil público de cualquier usuario por su ID.
        /// Endpoint: GET api/Usuario/perfil/{usuarioId}
        /// </summary>
        public async Task<JsonElement?> GetPerfilPublicoAsync(string usuarioId)
        {
            try
            {
                SetToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/Usuario/perfil/{usuarioId}");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(json).RootElement;
            }
            catch { return null; }
        }

        public async Task<bool> ActualizarPerfilAsync(object datos)
        {
            SetToken();
            // WhenWritingNull asegura que fotoPerfil=null no se envía (y no borra la foto en la API)
            var opts = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var body = JsonSerializer.Serialize(datos, opts);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/Usuario/perfil", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ActualizarPerfilConFotoAsync(object datosSinFoto, string? rutaFotoLocal)
        {
            try
            {
                string? fotoUrl = null;

                if (!string.IsNullOrEmpty(rutaFotoLocal))
                {
                    fotoUrl = await SubirImagenAsync(rutaFotoLocal, "perfiles");
                    if (fotoUrl == null) return false;
                }

                var json = JsonSerializer.Serialize(datosSinFoto);
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();

                if (fotoUrl != null)
                {
                    // Añadir la URL de la foto al dict y reserializar
                    var dictObj = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
                    dictObj["fotoPerfil"] = fotoUrl;
                    json = JsonSerializer.Serialize(dictObj);
                }

                SetToken();
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{BaseUrl}/Usuario/perfil", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> ActualizarUbicacionAsync(double lat, double lng, string? direccion)
        {
            SetToken();
            var body = JsonSerializer.Serialize(new { latitud = lat, longitud = lng, direccion });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/Usuario/ubicacion", content);
            return response.IsSuccessStatusCode;
        }

        // ── MASCOTAS ─────────────────────────────────────────────────────────

        public async Task<List<JsonElement>> GetMascotasAsync()
        {
            SetToken();
            var response = await _httpClient.GetAsync($"{BaseUrl}/Mascota");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
        }

        public async Task<List<JsonElement>> GetMascotasPublicasAsync()
        {
            try
            {
                SetToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/Mascota/publicas");
                if (!response.IsSuccessStatusCode) return new();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
            }
            catch { return new(); }
        }

        /// <summary>
        /// Devuelve las mascotas públicas de un usuario concreto.
        /// Endpoint: GET api/Mascota/publicas?usuarioId={usuarioId}
        /// </summary>
        public async Task<List<JsonElement>> GetMascotasPublicasDeUsuarioAsync(string usuarioId)
        {
            try
            {
                SetToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/Mascota/publicas?usuarioId={usuarioId}");
                if (!response.IsSuccessStatusCode) return new();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
            }
            catch { return new(); }
        }

        /// <summary>
        /// Crea una mascota en el backend. Devuelve el ID asignado (>0) o 0 si falla.
        /// </summary>
        public async Task<int> CrearMascotaAsync(object mascotaSinFoto, string? fotoRutaLocal = null)
        {
            try
            {
                SetToken();
                string? fotoUrl = null;

                if (!string.IsNullOrEmpty(fotoRutaLocal))
                    fotoUrl = await SubirImagenAsync(fotoRutaLocal, "mascotas");

                var json = JsonSerializer.Serialize(mascotaSinFoto);
                var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                if (fotoUrl != null)
                    dict["fotoUrl"] = fotoUrl;

                var body = JsonSerializer.Serialize(dict);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/Mascota", content);
                if (!response.IsSuccessStatusCode) return 0;

                // El backend devuelve la mascota creada con su Id
                var respJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(respJson);
                if (doc.RootElement.TryGetProperty("id", out var idEl))
                    return idEl.GetInt32();
                return 0;
            }
            catch { return 0; }
        }

        /// <summary>
        /// Actualiza una mascota existente en el backend (requiere el ID del backend).
        /// </summary>
        public async Task<bool> ActualizarMascotaAsync(int apiId, object mascotaSinFoto, string? fotoRutaLocal = null)
        {
            try
            {
                SetToken();
                string? fotoUrl = null;

                if (!string.IsNullOrEmpty(fotoRutaLocal))
                    fotoUrl = await SubirImagenAsync(fotoRutaLocal, "mascotas");

                var json = JsonSerializer.Serialize(mascotaSinFoto);
                var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                if (fotoUrl != null)
                    dict["fotoUrl"] = fotoUrl;

                var body = JsonSerializer.Serialize(dict);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{BaseUrl}/Mascota/{apiId}", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> EliminarMascotaAsync(int id)
        {
            SetToken();
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/Mascota/{id}");
            return response.IsSuccessStatusCode;
        }

        // ── PUBLICACIONES (FEED) ─────────────────────────────────────────────

        public async Task<List<JsonElement>> GetPublicacionesAsync(int pagina = 1, int por = 20)
        {
            try
            {
                SetToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/Publicaciones?pagina={pagina}&por={por}");
                if (!response.IsSuccessStatusCode) return new();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
            }
            catch { return new(); }
        }

        /// <summary>
        /// Devuelve las publicaciones de un usuario concreto.
        /// Endpoint: GET api/Publicaciones/usuario/{usuarioId}
        /// </summary>
        public async Task<List<JsonElement>> GetPublicacionesDeUsuarioAsync(string usuarioId)
        {
            try
            {
                SetToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/Publicaciones/usuario/{usuarioId}");
                if (!response.IsSuccessStatusCode) return new();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
            }
            catch { return new(); }
        }

        public async Task<int> CrearPublicacionAsync(string rutaLocalOBase64, string? descripcion = null)
        {
            try
            {
                SetToken();
                var imagenUrl = await SubirImagenAsync(rutaLocalOBase64, "publicaciones");
                if (imagenUrl == null) return 0;

                var body = JsonSerializer.Serialize(new { imagenUrl, descripcion });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/Publicaciones", content);
                if (!response.IsSuccessStatusCode) return 0;

                var json = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(json).RootElement.GetProperty("id").GetInt32();
            }
            catch { return 0; }
        }

        public async Task<(bool meGusta, int numLikes)> ToggleLikePublicacionAsync(int publicacionId)
        {
            try
            {
                SetToken();
                var response = await _httpClient.PostAsync($"{BaseUrl}/Publicaciones/{publicacionId}/like", null);
                if (!response.IsSuccessStatusCode) return (false, 0);
                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;
                return (root.GetProperty("meGusta").GetBoolean(), root.GetProperty("numLikes").GetInt32());
            }
            catch { return (false, 0); }
        }

        public async Task<List<JsonElement>> GetComentariosAsync(int publicacionId)
        {
            try
            {
                SetToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/Publicaciones/{publicacionId}/comentarios");
                if (!response.IsSuccessStatusCode) return new();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
            }
            catch { return new(); }
        }

        public async Task<JsonElement?> ComentarPublicacionAsync(int publicacionId, string contenido)
        {
            try
            {
                SetToken();
                var body = JsonSerializer.Serialize(new { contenido });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/Publicaciones/{publicacionId}/comentarios", content);
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(json).RootElement;
            }
            catch { return null; }
        }

        public async Task<bool> EliminarPublicacionAsync(int id)
        {
            try
            {
                SetToken();
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/Publicaciones/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> ActualizarDescripcionPublicacionAsync(int id, string descripcion)
        {
            try
            {
                SetToken();
                var body = JsonSerializer.Serialize(new { descripcion });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{BaseUrl}/Publicaciones/{id}", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ── FAVORITOS ────────────────────────────────────────────────────────

        public async Task<bool> AgregarFavoritoAsync(string cuidadorId)
        {
            SetToken();
            var response = await _httpClient.PostAsync($"{BaseUrl}/Favorito/{cuidadorId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EliminarFavoritoAsync(string cuidadorId)
        {
            SetToken();
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/Favorito/{cuidadorId}");
            return response.IsSuccessStatusCode;
        }
        public async Task<JsonElement?> GetTarjetaUsuarioAsync(string usuarioId)
        {
            try
            {
                SetToken();
                var resp = await _httpClient.GetAsync($"{BaseUrl}/Usuario/tarjeta/{usuarioId}");
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                return JsonDocument.Parse(json).RootElement;
            }
            catch { return null; }
        }

        public async Task<List<FavoritoItem>> GetFavoritosAsync()
        {
            try
            {
                SetToken();
                var resp = await _httpClient.GetAsync($"{BaseUrl}/Favorito");
                if (!resp.IsSuccessStatusCode) return new();
                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<FavoritoItem>>(json, _jsonOptions) ?? new();
            }
            catch { return new(); }
        }
        // ── MENSAJES ─────────────────────────────────────────────────────────

        public async Task<List<JsonElement>> GetConversacionesAsync()
        {
            SetToken();
            var response = await _httpClient.GetAsync($"{BaseUrl}/Mensaje/conversaciones");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
        }

        public async Task<List<JsonElement>> GetMensajesAsync(string otroUsuarioId)
        {
            SetToken();
            var response = await _httpClient.GetAsync($"{BaseUrl}/Mensaje/{otroUsuarioId}");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
        }

        public async Task<bool> EnviarMensajeAsync(string receptorId, string contenido)
        {
            SetToken();
            var body = JsonSerializer.Serialize(new { receptorId, contenido });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/Mensaje", content);
            return response.IsSuccessStatusCode;
        }

        // ── REVIEWS ──────────────────────────────────────────────────────────

        public async Task<ReviewResultado?> GetMisReviewsAsync()
        {
            try
            {
                SetToken();
                var resp = await _httpClient.GetAsync($"{BaseUrl}/Reviews/mias");
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ReviewResultado>(json, _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<ReviewResultado?> GetReviewsDeUsuarioAsync(string usuarioId)
        {
            try
            {
                SetToken();
                var resp = await _httpClient.GetAsync($"{BaseUrl}/Reviews/{usuarioId}");
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ReviewResultado>(json, _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<bool> CrearReviewAsync(string cuidadorId, int puntuacion, string comentario, int? ofertaId = null)
        {
            try
            {
                SetToken();
                var body = JsonSerializer.Serialize(new { cuidadorId, puntuacion, comentario, ofertaId });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = await _httpClient.PostAsync($"{BaseUrl}/Reviews", content);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ── OFERTAS ───────────────────────────────────────────────────────────

        public async Task<List<TaskItem>> GetOfertasAsync(bool? esCuidador = null)
        {
            try
            {
                SetToken();
                var url = $"{BaseUrl}/Ofertas";
                if (esCuidador.HasValue)
                    url += $"?esCuidador={esCuidador.Value.ToString().ToLower()}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new();

                var json = await response.Content.ReadAsStringAsync();
                var ofertas = JsonSerializer.Deserialize<List<OfertaDto>>(json, _jsonOptions) ?? new();
                return ofertas.Select(MapearATaskItem).ToList();
            }
            catch { return new(); }
        }

        /// <summary>
        /// Devuelve las ofertas activas de un usuario concreto.
        /// Endpoint: GET api/Ofertas/usuario/{usuarioId}
        /// </summary>
        public async Task<List<JsonElement>> GetOfertasDeUsuarioAsync(string usuarioId)
        {
            try
            {
                SetToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/Ofertas/usuario/{usuarioId}");
                if (!response.IsSuccessStatusCode) return new();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
            }
            catch { return new(); }
        }

        public async Task<int> CrearOfertaAsync(TaskItem task)
        {
            try
            {
                SetToken();
                var body = JsonSerializer.Serialize(MapearADto(task));
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/Ofertas", content);
                if (!response.IsSuccessStatusCode) return 0;
                var json = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(json).RootElement.GetProperty("id").GetInt32();
            }
            catch { return 0; }
        }

        public async Task<bool> ActualizarOfertaAsync(int id, TaskItem task)
        {
            try
            {
                SetToken();
                var body = JsonSerializer.Serialize(MapearADto(task));
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{BaseUrl}/Ofertas/{id}", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> FinalizarOfertaAsync(int id)
        {
            try
            {
                SetToken();
                var response = await _httpClient.PutAsync($"{BaseUrl}/Ofertas/{id}/finalizar", null);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> AceptarSolicitanteAsync(int ofertaId, string solicitanteId)
        {
            try
            {
                SetToken();
                var response = await _httpClient.PutAsync(
                    $"{BaseUrl}/Ofertas/{ofertaId}/aceptar/{solicitanteId}", null);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> EliminarOfertaAsync(int id)
        {
            try
            {
                SetToken();
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/Ofertas/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ── SOLICITUDES ───────────────────────────────────────────────────────

        public async Task<List<SolicitudItem>> GetMisSolicitudesAsync()
        {
            try
            {
                SetToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/Solicitudes/mias");
                if (!response.IsSuccessStatusCode) return new();
                var json = await response.Content.ReadAsStringAsync();
                var lista = JsonSerializer.Deserialize<List<SolicitudApiDto>>(json, _jsonOptions) ?? new();
                return lista.Select(MapearSolicitud).ToList();
            }
            catch { return new(); }
        }

        public async Task<List<SolicitudRecibidaItem>> GetSolicitudesRecibidasAsync(int? ofertaId = null)
        {
            try
            {
                SetToken();
                var url = $"{BaseUrl}/Solicitudes/recibidas";
                if (ofertaId.HasValue) url += $"?ofertaId={ofertaId}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new();
                var json = await response.Content.ReadAsStringAsync();
                var lista = JsonSerializer.Deserialize<List<SolicitudRecibidaApiDto>>(json, _jsonOptions) ?? new();
                return lista.Select(s => new SolicitudRecibidaItem
                {
                    Id = s.Id,
                    Estado = s.Estado ?? "Pendiente",
                    Mensaje = s.Mensaje,
                    FechaCreacion = s.FechaCreacion,
                    SolicitanteId = s.Solicitante?.Id ?? string.Empty,
                    SolicitanteNombre = s.Solicitante?.Nombre ?? string.Empty,
                    SolicitanteFoto = s.Solicitante?.Foto,
                    OfertaId = s.Oferta?.Id ?? 0,
                    OfertaTitulo = s.Oferta?.Titulo ?? string.Empty,
                }).ToList();
            }
            catch { return new(); }
        }

        public async Task<bool> EnviarSolicitudAsync(int ofertaId, string? mensaje = null)
        {
            try
            {
                SetToken();
                var body = JsonSerializer.Serialize(new { ofertaId, mensaje });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/Solicitudes", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> CancelarSolicitudAsync(int solicitudId)
        {
            try
            {
                SetToken();
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/Solicitudes/{solicitudId}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ── CUENTA ───────────────────────────────────────────────────────────

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { email });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = await _httpClient.PostAsync($"{BaseUrl}/Auth/forgot-password", content);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> CambiarPasswordAsync(string passwordActual, string nuevoPassword)
        {
            try
            {
                SetToken();
                var body = JsonSerializer.Serialize(new { passwordActual, nuevoPassword });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = await _httpClient.PostAsync($"{BaseUrl}/Auth/cambiar-password", content);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> ReenviarVerificacionAsync(string email)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { email });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = await _httpClient.PostAsync($"{BaseUrl}/Auth/reenviar-verificacion", content);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> GuardarPlayerIdAsync(string playerId)
        {
            try
            {
                SetToken();
                var body = JsonSerializer.Serialize(new { playerId });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = await _httpClient.PutAsync($"{BaseUrl}/Usuario/player-id", content);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

         public async Task<bool> EliminarCuentaAsync()
        {
            try
            {
                SetToken();
                var resp = await _httpClient.DeleteAsync($"{BaseUrl}/Auth/cuenta");
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<LoginResult?> LoginConGoogleAsync(string idToken)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { idToken });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/Auth/google", content);
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;
                return new LoginResult
                {
                    Token = root.GetProperty("token").GetString() ?? "",
                    UserId = root.GetProperty("userId").GetString() ?? "",
                    NombreCompleto = root.GetProperty("nombreCompleto").GetString() ?? "",
                    Email = root.GetProperty("email").GetString() ?? "",
                    EsPropietario = root.GetProperty("esPropietario").GetBoolean(),
                    EsCuidador = root.GetProperty("esCuidador").GetBoolean(),
                    FotoPerfil = root.TryGetProperty("fotoPerfil", out var fp) ? fp.GetString() : null,
                    EsNuevo = root.TryGetProperty("esNuevo", out var en) && en.GetBoolean(),
                };
            }
            catch { return null; }
        }

        public async Task<TaskItem?> GetOfertaByIdAsync(int id)
        {
            try
            {
                SetToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/Ofertas/{id}");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<OfertaDto>(json, _jsonOptions);
                return dto == null ? null : MapearATaskItem(dto);
            }
            catch { return null; }
        }

        // ── Mappers ───────────────────────────────────────────────────────────

        public static TaskItem MapearATaskItem(OfertaDto o) => new()
        {
            offerId = o.Id,
            caretaker = o.EsCuidador,
            title = o.Titulo ?? string.Empty,
            description = o.Descripcion ?? string.Empty,
            tags = o.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new(),
            perHour = o.TarifaPorHora ?? 0,
            totalPrice = o.PrecioTotal ?? 0,
            date0 = o.FechaInicio,
            date1 = o.FechaFin,
            exactHours = o.HorasExactas,
            timeSpan0 = o.HoraDesde,
            timeSpan1 = o.HoraHasta,
            weekDays = o.DiasDisponibles?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new(),
            timeOfDay = o.FranjasHorarias?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new(),
            hadPets = o.HaTenidoMascotas,
            maxPets = o.MaxMascotas,
            canLookafter = o.TiposAnimal?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new(),
            petList = o.ListaMascotas?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new(),
            specialNeeds = o.NecesidadesEspeciales,
            specialNeedsDes = o.DescripcionNecesidades ?? string.Empty,
            specialNeedsXP = o.ExperienciaNecesidades ?? string.Empty,
            photoPath = o.FotoPath ?? string.Empty,
            userLocation = o.Localizacion ?? string.Empty,
            latitud = o.Latitud,
            longitud = o.Longitud,
            name = o.Autor?.Nombre ?? string.Empty,
            age = o.Autor?.Edad ?? 0,
            FotoAutor = o.Autor?.Foto,
            ValoracionAutor = (decimal)(o.Autor?.Valoracion ?? 0),
            LocAutor = o.Autor?.Localizacion,
            favorites = o.Favoritos,
            EsMia = o.EsMia,
            EsAceptado = o.EsAceptado,
            AceptadoPorId = o.AceptadoPorId,
            UsuarioIdPublico = o.UsuarioId,
            IsFinished = o.Finalizada,
        };

        private static object MapearADto(TaskItem t) => new
        {
            esCuidador = t.caretaker,
            titulo = t.title,
            descripcion = t.description,
            tags = t.tags.Count > 0 ? string.Join(",", t.tags) : null,
            tarifaPorHora = t.caretaker ? t.perHour : (decimal?)null,
            precioTotal = !t.caretaker ? t.totalPrice : (decimal?)null,
            fechaInicio = t.date0,
            fechaFin = t.date1,
            horasExactas = t.exactHours,
            horaDesde = t.timeSpan0,
            horaHasta = t.timeSpan1,
            diasDisponibles = t.weekDays.Count > 0 ? string.Join(",", t.weekDays) : null,
            franjasHorarias = t.timeOfDay.Count > 0 ? string.Join(",", t.timeOfDay) : null,
            haTenidoMascotas = t.hadPets,
            maxMascotas = t.maxPets,
            tiposAnimal = t.canLookafter.Count > 0 ? string.Join(",", t.canLookafter) : null,
            listaMascotas = t.petList.Count > 0 ? string.Join(",", t.petList) : null,
            necesidadesEspeciales = t.specialNeeds,
            descripcionNecesidades = t.specialNeedsDes,
            experienciaNecesidades = t.specialNeedsXP,
        };

        private static SolicitudItem MapearSolicitud(SolicitudApiDto s) => new()
        {
            Id = s.Id,
            Estado = s.Estado ?? "Pendiente",
            Mensaje = s.Mensaje,
            FechaCreacion = s.FechaCreacion,
            FechaRespuesta = s.FechaRespuesta,
            OfertaId = s.Oferta?.Id ?? 0,
            OfertaEsCuidador = s.Oferta?.EsCuidador ?? false,
            OfertaTitulo = s.Oferta?.Titulo ?? string.Empty,
            OfertaDescripcion = s.Oferta?.Descripcion ?? string.Empty,
            OfertaLocalizacion = s.Oferta?.Localizacion,
            OfertaTarifaPorHora = s.Oferta?.TarifaPorHora,
            OfertaPrecioTotal = s.Oferta?.PrecioTotal,
            OfertaFinalizada = s.Oferta?.Finalizada ?? false,
            AutorNombre = s.Oferta?.Autor?.Nombre ?? string.Empty,
            AutorFoto = s.Oferta?.Autor?.Foto,
        };
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    public class ReviewResultado
    {
        [JsonPropertyName("media")] public double Media { get; set; }
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("reviews")] public List<ReviewItem> Reviews { get; set; } = new();
    }

    public class ReviewItem
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("puntuacion")] public int Puntuacion { get; set; }
        [JsonPropertyName("comentario")] public string Comentario { get; set; } = "";
        [JsonPropertyName("fechaCreacion")] public DateTime FechaCreacion { get; set; }
        [JsonPropertyName("autorNombre")] public string AutorNombre { get; set; } = "";
        [JsonPropertyName("autorFoto")] public string? AutorFoto { get; set; }
        [JsonPropertyName("autorId")] public string AutorId { get; set; } = "";
    }

    public class OfertaDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("esCuidador")] public bool EsCuidador { get; set; }
        [JsonPropertyName("titulo")] public string? Titulo { get; set; }
        [JsonPropertyName("descripcion")] public string? Descripcion { get; set; }
        [JsonPropertyName("fotoPath")] public string? FotoPath { get; set; }
        [JsonPropertyName("tags")] public string? Tags { get; set; }
        [JsonPropertyName("tarifaPorHora")] public decimal? TarifaPorHora { get; set; }
        [JsonPropertyName("precioTotal")] public decimal? PrecioTotal { get; set; }
        [JsonPropertyName("fechaInicio")] public DateTime? FechaInicio { get; set; }
        [JsonPropertyName("fechaFin")] public DateTime? FechaFin { get; set; }
        [JsonPropertyName("horasExactas")] public bool HorasExactas { get; set; }
        [JsonPropertyName("horaDesde")] public TimeSpan? HoraDesde { get; set; }
        [JsonPropertyName("horaHasta")] public TimeSpan? HoraHasta { get; set; }
        [JsonPropertyName("diasDisponibles")] public string? DiasDisponibles { get; set; }
        [JsonPropertyName("franjasHorarias")] public string? FranjasHorarias { get; set; }
        [JsonPropertyName("haTenidoMascotas")] public bool HaTenidoMascotas { get; set; }
        [JsonPropertyName("maxMascotas")] public int MaxMascotas { get; set; }
        [JsonPropertyName("tiposAnimal")] public string? TiposAnimal { get; set; }
        [JsonPropertyName("listaMascotas")] public string? ListaMascotas { get; set; }
        [JsonPropertyName("necesidadesEspeciales")] public bool NecesidadesEspeciales { get; set; }
        [JsonPropertyName("descripcionNecesidades")] public string? DescripcionNecesidades { get; set; }
        [JsonPropertyName("experienciaNecesidades")] public string? ExperienciaNecesidades { get; set; }
        [JsonPropertyName("localizacion")] public string? Localizacion { get; set; }
        [JsonPropertyName("latitud")] public double? Latitud { get; set; }
        [JsonPropertyName("longitud")] public double? Longitud { get; set; }
        [JsonPropertyName("fechaCreacion")] public DateTime FechaCreacion { get; set; }
        [JsonPropertyName("activa")] public bool Activa { get; set; }
        [JsonPropertyName("finalizada")] public bool Finalizada { get; set; }
        [JsonPropertyName("favoritos")] public int Favoritos { get; set; }
        [JsonPropertyName("aceptadoPorId")] public string? AceptadoPorId { get; set; }
        [JsonPropertyName("usuarioId")] public string? UsuarioId { get; set; }
        [JsonPropertyName("esMia")] public bool EsMia { get; set; }
        [JsonPropertyName("esAceptado")] public bool EsAceptado { get; set; }
        [JsonPropertyName("autor")] public AutorDto? Autor { get; set; }
    }

    public class AutorDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("nombre")] public string? Nombre { get; set; }
        [JsonPropertyName("fotoPerfil")] public string? Foto { get; set; }
        [JsonPropertyName("valoracion")] public double? Valoracion { get; set; }
        [JsonPropertyName("tarifaPorHora")] public decimal? TarifaPorHora { get; set; }
        [JsonPropertyName("esCuidador")] public bool EsCuidador { get; set; }
        [JsonPropertyName("esPropietario")] public bool EsPropietario { get; set; }
        [JsonPropertyName("edad")] public int? Edad { get; set; }
        [JsonPropertyName("localizacion")] public string? Localizacion { get; set; }
    }

    public class SolicitudApiDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("estado")] public string? Estado { get; set; }
        [JsonPropertyName("mensaje")] public string? Mensaje { get; set; }
        [JsonPropertyName("fechaCreacion")] public DateTime FechaCreacion { get; set; }
        [JsonPropertyName("fechaRespuesta")] public DateTime? FechaRespuesta { get; set; }
        [JsonPropertyName("oferta")] public SolicitudOfertaDto? Oferta { get; set; }
    }

    public class SolicitudOfertaDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("esCuidador")] public bool EsCuidador { get; set; }
        [JsonPropertyName("titulo")] public string? Titulo { get; set; }
        [JsonPropertyName("descripcion")] public string? Descripcion { get; set; }
        [JsonPropertyName("localizacion")] public string? Localizacion { get; set; }
        [JsonPropertyName("tarifaPorHora")] public decimal? TarifaPorHora { get; set; }
        [JsonPropertyName("precioTotal")] public decimal? PrecioTotal { get; set; }
        [JsonPropertyName("finalizada")] public bool Finalizada { get; set; }
        [JsonPropertyName("autor")] public AutorDto? Autor { get; set; }
    }

    public class SolicitudRecibidaApiDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("estado")] public string? Estado { get; set; }
        [JsonPropertyName("mensaje")] public string? Mensaje { get; set; }
        [JsonPropertyName("fechaCreacion")] public DateTime FechaCreacion { get; set; }
        [JsonPropertyName("solicitante")] public SolicitanteDto? Solicitante { get; set; }
        [JsonPropertyName("oferta")] public SolicitudOfertaDto? Oferta { get; set; }
    }

    public class SolicitanteDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("nombre")] public string? Nombre { get; set; }
        [JsonPropertyName("foto")] public string? Foto { get; set; }
    }

    public class PerfilDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("nombreCompleto")] public string? NombreCompleto { get; set; }
        [JsonPropertyName("apellidos")] public string? Apellidos { get; set; }
        [JsonPropertyName("fechaNacimiento")] public DateTime? FechaNacimiento { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("fotoPerfil")] public string? FotoPerfil { get; set; }
        [JsonPropertyName("bio")] public string? Bio { get; set; }
        [JsonPropertyName("idioma")] public string? Idioma { get; set; }
        [JsonPropertyName("esPropietario")] public bool EsPropietario { get; set; }
        [JsonPropertyName("esCuidador")] public bool EsCuidador { get; set; }
        [JsonPropertyName("latitud")] public double? Latitud { get; set; }
        [JsonPropertyName("longitud")] public double? Longitud { get; set; }
        [JsonPropertyName("direccion")] public string? Direccion { get; set; }
        [JsonPropertyName("tarifaPorHora")] public decimal? TarifaPorHora { get; set; }
        [JsonPropertyName("diasDisponibles")] public string? DiasDisponibles { get; set; }
        [JsonPropertyName("franjasHorarias")] public string? FranjasHorarias { get; set; }
    }
}