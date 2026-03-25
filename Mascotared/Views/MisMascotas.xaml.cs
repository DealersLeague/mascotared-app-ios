using Mascotared.Models;
using Mascotared.Services;

namespace Mascotared.Views
{
    public partial class MisMascotas : ContentPage
    {
        private MascotasItem _mascota;
        private readonly HashSet<string> _tagsPersonalidad = new();
        private readonly HashSet<string> _condicionesSeleccionadas = new();

        private readonly List<string> _todosLosTags = new()
        {
            "Miedoso", "Escapista", "Glotón", "Ladrador",
            "Destructor", "Cariñoso", "Independiente", "Juguetón",
            "Tranquilo", "Protector"
        };

        private readonly List<string> _todasLasCondiciones = new()
        {
            "Ninguna", "Diabetes", "Displasia", "Epilepsia",
            "Insuficiencia renal", "Ceguera", "Sordera",
            "Hipotiroidismo", "Artrosis", "Cardiopatía", "Otra"
        };

        public MisMascotas()
        {
            InitializeComponent();
            _mascota = new MascotasItem();
            Title = "Nueva mascota";
            GenerarTagsPersonalidad();
            GenerarChipsCondiciones();
        }

        public MisMascotas(MascotasItem mascota) : this()
        {
            _mascota = mascota;
            Title = $"Editar · {mascota.Name}";
            CargarCampos(mascota);
        }

        // ── Chips personalidad ────────────────────────────────────────────────
        private void GenerarTagsPersonalidad()
        {
            FlexPersonalidad.Children.Clear();
            foreach (var tag in _todosLosTags)
            {
                bool activo = _tagsPersonalidad.Contains(tag);
                var chip = CrearChip(tag, activo, "#455AEB");
                var tap = new TapGestureRecognizer();
                var captura = tag;
                tap.Tapped += (s, e) =>
                {
                    if (_tagsPersonalidad.Contains(captura)) _tagsPersonalidad.Remove(captura);
                    else _tagsPersonalidad.Add(captura);
                    GenerarTagsPersonalidad();
                };
                chip.GestureRecognizers.Add(tap);
                FlexPersonalidad.Children.Add(chip);
            }
        }

        // ── Chips condiciones médicas ─────────────────────────────────────────
        private void GenerarChipsCondiciones()
        {
            FlexCondiciones.Children.Clear();
            foreach (var condicion in _todasLasCondiciones)
            {
                bool activo = _condicionesSeleccionadas.Contains(condicion);
                var chip = CrearChip(condicion, activo, "#FE3D7D");
                var tap = new TapGestureRecognizer();
                var captura = condicion;
                tap.Tapped += (s, e) =>
                {
                    if (captura == "Ninguna")
                    {
                        _condicionesSeleccionadas.Clear();
                        _condicionesSeleccionadas.Add("Ninguna");
                    }
                    else
                    {
                        _condicionesSeleccionadas.Remove("Ninguna");
                        if (_condicionesSeleccionadas.Contains(captura))
                            _condicionesSeleccionadas.Remove(captura);
                        else
                            _condicionesSeleccionadas.Add(captura);
                    }
                    GenerarChipsCondiciones();
                };
                chip.GestureRecognizers.Add(tap);
                FlexCondiciones.Children.Add(chip);
            }
        }

        private static Border CrearChip(string texto, bool activo, string colorActivo)
        {
            return new Border
            {
                BackgroundColor = activo ? Color.FromArgb(colorActivo) : Color.FromArgb("#F4F6FA"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
                Stroke = activo ? Colors.Transparent : Color.FromArgb("#D0D5EE"),
                StrokeThickness = activo ? 0 : 1,
                Padding = new Thickness(14, 7),
                Margin = new Thickness(0, 0, 8, 8),
                Content = new Label
                {
                    Text = texto,
                    TextColor = activo ? Colors.White : Color.FromArgb("#455AEB"),
                    FontFamily = "OpenSansSemibold",
                    FontSize = 13
                }
            };
        }

        // ── Cargar campos al editar ───────────────────────────────────────────
        private void CargarCampos(MascotasItem m)
        {
            EntryNombre.Text = m.Name;
            EntryRaza.Text = m.Race;
            EntryFrecAlim.Text = m.FrecuenciaAlimentacion;
            EntryMedHorario.Text = m.HorarioMedicacion;
            EntryVetNombre.Text = m.VeterinarioNombre;
            EntryVetDir.Text = m.VeterinarioDireccion;
            EntryVetTel.Text = m.VeterinarioTelefono;
            EntryColorMarcas.Text = m.ColorMarcas;
            EntryMicrochip.Text = m.Microchip;
            EntryDescripcion.Text = m.Description;
            EntryAlergias.Text = m.Alergias;
            EntryAlimProhibidos.Text = m.AlimentosProhibidos;
            EntryMiedos.Text = m.MiedosReactividad;
            EntryCentroUrgencias.Text = m.CentroUrgencias;
            EntryTelUrgencias.Text = m.TelefonoUrgencias;

            SetPicker(PickerEspecie, m.Especie);
            SetPicker(PickerSexo, m.Sexo);
            SetPicker(PickerReproductivo, m.EstadoReproductivo);
            SetPicker(PickerNivelEnergia, m.NivelEnergia);
            SetPicker(PickerMedicacion, m.TipoMedicacion);
            SetPicker(PickerUnidadPeso, m.UnidadPeso);
            SetPicker(PickerMovilidad, m.NivelMovilidad);
            SetPicker(PickerCorrea, m.TipoCorreaArnes);
            SetPicker(PickerPresupuesto, m.PresupuestoEmergencia);

            if (m.Peso.HasValue) EntryPeso.Text = m.Peso.Value.ToString();
            if (m.EdadAnios.HasValue) EntryEdadAnios.Text = m.EdadAnios.Value.ToString();
            if (m.EdadMeses.HasValue) EntryEdadMeses.Text = m.EdadMeses.Value.ToString();
            if (m.FechaNacimiento.HasValue) DatePickerNacimiento.Date = m.FechaNacimiento.Value;

            SwitchNinos.IsToggled = m.BienConNinos ?? false;
            SwitchMismaEspecie.IsToggled = m.BienConMismaEspecie ?? false;
            SwitchOtrasEspecies.IsToggled = m.BienConOtrasEspecies ?? false;
            SwitchInstintoPresa.IsToggled = m.InstintoPresa ?? false;
            SwitchSeguroRC.IsToggled = m.TieneSeguroRC;
            SwitchVacunas.IsToggled = m.TieneVacunasAlDia;

            if (!string.IsNullOrEmpty(m.DocumentoPath))
                LabelDocumento.Text = Path.GetFileName(m.DocumentoPath);
            if (!string.IsNullOrEmpty(m.PhotoPath))
                FotoMascota.Source = ImageSource.FromFile(m.PhotoPath);

            // Tags personalidad
            if (!string.IsNullOrEmpty(m.Tags))
                foreach (var t in m.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    _tagsPersonalidad.Add(t);
            GenerarTagsPersonalidad();

            // Condiciones médicas
            if (!string.IsNullOrEmpty(m.CondicionesMedicas))
                foreach (var c in m.CondicionesMedicas.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    _condicionesSeleccionadas.Add(c);
            GenerarChipsCondiciones();

            // Específicos
            EntryVuelo.Text = m.TipoVuelo;
            EntryCantosOPalabras.Text = m.CantosOPalabras;
            EntryHumedad.Text = m.HumedadNecesaria;
            EntryHorasUVAves.Text = m.HorasLuzUV?.ToString();
            EntryTempDia.Text = m.RangoTemperaturaDia;
            EntryTempNoche.Text = m.RangoTemperaturaNoche;
            EntryHorasUV.Text = m.HorasLuzUV?.ToString();
            SetPicker(PickerHabitat, m.TipoHabitat);
            EntryCapa.Text = m.Capa;
            EntryHierro.Text = m.HierroGanaderia;
            SetPicker(PickerUso, m.Uso);
            EntryAlzada.Text = m.Alzada?.ToString();
            SetPicker(PickerHerrado, m.EstadoHerrado);
            SetPicker(PickerSustrato, m.TipoSustrato);
            EntryTiempoJaula.Text = m.TiempoFueraJaula?.ToString();
            SetPicker(PickerArena, m.TipoArena);
        }

        private static void SetPicker(Picker picker, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            int idx = picker.Items.IndexOf(value);
            if (idx >= 0) picker.SelectedIndex = idx;
        }

        // ── Especie cambiada → mostrar secciones dinámicas ───────────────────
        private void OnEspecieCambiada(object sender, EventArgs e)
        {
            string especie = PickerEspecie.SelectedItem?.ToString() ?? "";
            SeccionPerros.IsVisible = especie == "Perro";
            SeccionGatos.IsVisible = especie == "Gato";
            SeccionAves.IsVisible = especie == "Ave";
            SeccionReptiles.IsVisible = especie == "Reptil";
            SeccionCaballos.IsVisible = especie == "Caballo";
            SeccionPequenos.IsVisible = especie is "Conejo" or "Hámster";

            LabelIdentificacion.Text = especie switch
            {
                "Ave" => "Número de anilla",
                "Reptil" => "Microchip / CITES",
                _ => "Número de microchip"
            };
        }

        // ── Foto ─────────────────────────────────────────────────────────────
        private async void OnSeleccionarFotoTapped(object sender, EventArgs e)
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync();
                if (result is null) return;
                FotoMascota.Source = ImageSource.FromFile(result.FullPath);
                _mascota.PhotoPath = result.FullPath;
            }
            catch (Exception ex) { await DisplayAlertAsync("Error", ex.Message, "OK"); }
        }

        // ── Documento ────────────────────────────────────────────────────────
        private async void OnSubirDocumentoTapped(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Selecciona documento",
                    FileTypes = FilePickerFileType.Pdf
                });
                if (result is null) return;
                LabelDocumento.Text = result.FileName;
                _mascota.DocumentoPath = result.FullPath;
            }
            catch (Exception ex) { await DisplayAlertAsync("Error", ex.Message, "OK"); }
        }

        // ── Guardar ──────────────────────────────────────────────────────────
        private async void OnGuardarTapped(object sender, EventArgs e)
        {
            if (PickerEspecie.SelectedIndex < 0)
            {
                await DisplayAlertAsync("Campo requerido", "Selecciona la especie de la mascota.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(EntryNombre.Text))
            {
                await DisplayAlertAsync("Campo requerido", "Introduce el nombre de la mascota.", "OK");
                return;
            }

            // Básicos
            _mascota.Name = EntryNombre.Text.Trim();
            _mascota.Race = EntryRaza.Text?.Trim();
            _mascota.Especie = PickerEspecie.SelectedItem?.ToString();
            _mascota.Sexo = PickerSexo.SelectedItem?.ToString();
            _mascota.EstadoReproductivo = PickerReproductivo.SelectedItem?.ToString();
            _mascota.NivelEnergia = PickerNivelEnergia.SelectedItem?.ToString();
            _mascota.TipoMedicacion = PickerMedicacion.SelectedItem?.ToString();
            _mascota.UnidadPeso = PickerUnidadPeso.SelectedItem?.ToString();
            _mascota.FrecuenciaAlimentacion = EntryFrecAlim.Text?.Trim();
            _mascota.HorarioMedicacion = EntryMedHorario.Text?.Trim();
            _mascota.VeterinarioNombre = EntryVetNombre.Text?.Trim();
            _mascota.VeterinarioDireccion = EntryVetDir.Text?.Trim();
            _mascota.VeterinarioTelefono = EntryVetTel.Text?.Trim();
            _mascota.ColorMarcas = EntryColorMarcas.Text?.Trim();
            _mascota.Microchip = EntryMicrochip.Text?.Trim();
            _mascota.Description = EntryDescripcion.Text?.Trim();
            _mascota.FechaNacimiento = DatePickerNacimiento.Date == default ? null : DatePickerNacimiento.Date;

            // Salud crítica
            _mascota.Alergias = EntryAlergias.Text?.Trim();
            _mascota.AlimentosProhibidos = EntryAlimProhibidos.Text?.Trim();
            _mascota.CondicionesMedicas = string.Join(",", _condicionesSeleccionadas);
            _mascota.NivelMovilidad = PickerMovilidad.SelectedItem?.ToString();

            // Comportamiento
            _mascota.MiedosReactividad = EntryMiedos.Text?.Trim();
            _mascota.TipoCorreaArnes = PickerCorrea.SelectedItem?.ToString();
            _mascota.InstintoPresa = SwitchInstintoPresa.IsToggled;
            _mascota.BienConNinos = SwitchNinos.IsToggled;
            _mascota.BienConMismaEspecie = SwitchMismaEspecie.IsToggled;
            _mascota.BienConOtrasEspecies = SwitchOtrasEspecies.IsToggled;

            // Personalidad
            _mascota.Tags = string.Join(",", _tagsPersonalidad);

            // Emergencia
            _mascota.CentroUrgencias = EntryCentroUrgencias.Text?.Trim();
            _mascota.TelefonoUrgencias = EntryTelUrgencias.Text?.Trim();
            _mascota.PresupuestoEmergencia = PickerPresupuesto.SelectedItem?.ToString();

            // Documentación
            _mascota.TieneSeguroRC = SwitchSeguroRC.IsToggled;
            _mascota.TieneVacunasAlDia = SwitchVacunas.IsToggled;

            // Numéricos
            if (decimal.TryParse(EntryPeso.Text, out decimal peso)) _mascota.Peso = peso;
            if (int.TryParse(EntryEdadAnios.Text, out int anios)) _mascota.EdadAnios = anios;
            if (int.TryParse(EntryEdadMeses.Text, out int meses)) _mascota.EdadMeses = meses;

            // Específicos por especie
            _mascota.TipoVuelo = EntryVuelo.Text?.Trim();
            _mascota.CantosOPalabras = EntryCantosOPalabras.Text?.Trim();
            _mascota.HumedadNecesaria = EntryHumedad.Text?.Trim();
            if (decimal.TryParse(EntryHorasUVAves.Text, out decimal uvAves))
                _mascota.HorasLuzUV = uvAves;

            _mascota.TipoHabitat = PickerHabitat.SelectedItem?.ToString();
            _mascota.RangoTemperaturaDia = EntryTempDia.Text?.Trim();
            _mascota.RangoTemperaturaNoche = EntryTempNoche.Text?.Trim();
            if (decimal.TryParse(EntryHorasUV.Text, out decimal uv))
                _mascota.HorasLuzUV = uv;

            _mascota.Capa = EntryCapa.Text?.Trim();
            _mascota.HierroGanaderia = EntryHierro.Text?.Trim();
            _mascota.Uso = PickerUso.SelectedItem?.ToString();
            _mascota.EstadoHerrado = PickerHerrado.SelectedItem?.ToString();
            if (decimal.TryParse(EntryAlzada.Text, out decimal alzada))
                _mascota.Alzada = alzada;

            _mascota.TipoSustrato = PickerSustrato.SelectedItem?.ToString();
            if (decimal.TryParse(EntryTiempoJaula.Text, out decimal jaula))
                _mascota.TiempoFueraJaula = jaula;

            _mascota.TipoArena = PickerArena.SelectedItem?.ToString();

            // ── Asociar al usuario activo ─────────────────────────────────────
            if (string.IsNullOrEmpty(_mascota.UsuarioId))
                _mascota.UsuarioId = Preferences.Get("user_id", string.Empty);

            // ── Guardar localmente ────────────────────────────────────────────
            await MascotaRepository.Instance.SaveOrUpdateAsync(_mascota);

            // ── Sincronizar con el backend (para que sea pública) ─────────────
            try
            {
                var api = new Mascotared.Services.ApiService();
                var dto = new
                {
                    Nombre = _mascota.Name,
                    Especie = _mascota.Especie,
                    Raza = _mascota.Race,
                    FechaNacimiento = _mascota.FechaNacimiento,
                    Descripcion = _mascota.Description,
                    CuidadosEspeciales = _mascota.CuidadosEspeciales,
                };

                string? fotoLocal = string.IsNullOrEmpty(_mascota.PhotoPath) ? null : _mascota.PhotoPath;

                if (_mascota.ApiId == 0)
                {
                    // Nueva mascota → crear en el backend
                    int newApiId = await api.CrearMascotaAsync(dto, fotoLocal);
                    if (newApiId > 0)
                    {
                        _mascota.ApiId = newApiId;
                        await MascotaRepository.Instance.SaveOrUpdateAsync(_mascota);
                    }
                }
                else
                {
                    // Edición → actualizar en el backend
                    await api.ActualizarMascotaAsync(_mascota.ApiId, dto, fotoLocal);
                }
            }
            catch { /* No bloquear si la API falla */ }

            await DisplayAlertAsync("✅ Guardado", $"{_mascota.Name} guardado correctamente.", "OK");
            await Navigation.PopAsync();
        }
    }
}