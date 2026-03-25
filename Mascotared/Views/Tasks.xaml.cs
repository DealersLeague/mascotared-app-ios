using Mascotared.Models;
using Mascotared.Services;
using System.Collections.ObjectModel;

namespace Mascotared.Views;

public partial class Tasks : ContentPage
{
    bool exception;
    private readonly ApiService _api = new();

    public ObservableCollection<string> WeekDaysList { get; } = new() { "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sábado", "Domingo" };
    private List<string> weekList = new();

    public ObservableCollection<string> TimeOfDayList { get; } = new() { "Mañana", "Mediodía", "Tarde", "Noche" };
    private List<string> timeList = new();

    public ObservableCollection<string> PetType { get; } = new() { "Perro", "Gato", "Ave", "Reptil", "Conejo", "Caballo", "Hámster", "Otros" };
    private List<string> petTypeList = new();

    public ObservableCollection<MascotaItem> UserPetList { get; } = new();

    private List<string> tags = new();
    private TaskItem? _taskParaEditar;

    // Constructor nueva tarea
    public Tasks()
    {
        InitializeComponent();
    }

    // Constructor editar tarea existente
    public Tasks(TaskItem task)
    {
        _taskParaEditar = task;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarMascotasDesdeApiAsync();

        if (_taskParaEditar != null)
            RellenarFormulario(_taskParaEditar);
    }

    // ── Carga mascotas del usuario (repo local, igual que PerfilConfigUser) ───
    private async Task CargarMascotasDesdeApiAsync()
    {
        UserPetList.Clear();
        UsuarioService.Instancia.Usuario.Mascotas.Clear();

        // 1. Fuente principal: repositorio local (mascotas.json)
        //    Mismo origen que MisMascotasLista y PerfilConfigUser
        try
        {
            string miId = Preferences.Get("user_id", string.Empty);
            var todas = await MascotaRepository.Instance.GetAllAsync();
            var misMascotas = todas
                .Where(m => string.IsNullOrEmpty(m.UsuarioId) || m.UsuarioId == miId)
                .ToList();

            foreach (var m in misMascotas)
            {
                var item = new MascotaItem
                {
                    Nombre = m.Name,
                    TipoAnimal = m.Especie,
                    Raza = m.Race ?? string.Empty
                };
                UserPetList.Add(item);
                UsuarioService.Instancia.Usuario.Mascotas.Add(item);
            }
        }
        catch { /* continuar con siguiente fuente */ }

        // Actualizar banner de estado vacío
        SinMascotasBanner.IsVisible = UserPetList.Count == 0;

        // 2. Si el repo local está vacío, intentar API remota como refuerzo
        if (UserPetList.Count == 0)
        {
            try
            {
                var mascotas = await _api.GetMascotasAsync();
                foreach (var m in mascotas)
                {
                    string nombre = "";
                    if (m.TryGetProperty("nombre", out var n1)) nombre = n1.GetString() ?? "";
                    else if (m.TryGetProperty("Nombre", out var n2)) nombre = n2.GetString() ?? "";
                    if (string.IsNullOrEmpty(nombre)) continue;

                    string tipo = "";
                    if (m.TryGetProperty("tipoAnimal", out var t1)) tipo = t1.GetString() ?? "";
                    else if (m.TryGetProperty("especie", out var t2)) tipo = t2.GetString() ?? "";
                    else if (m.TryGetProperty("TipoAnimal", out var t3)) tipo = t3.GetString() ?? "";

                    string raza = "";
                    if (m.TryGetProperty("raza", out var r1)) raza = r1.GetString() ?? "";
                    else if (m.TryGetProperty("Raza", out var r2)) raza = r2.GetString() ?? "";

                    var item = new MascotaItem { Nombre = nombre, TipoAnimal = tipo, Raza = raza };
                    UserPetList.Add(item);
                    UsuarioService.Instancia.Usuario.Mascotas.Add(item);
                }
            }
            catch { /* silencioso */ }
            finally { SinMascotasBanner.IsVisible = UserPetList.Count == 0; }
        }
    }

    // ── Rellena el formulario al editar una tarea existente ───────────────────
    private void RellenarFormulario(TaskItem t)
    {
        propForm.IsVisible = !t.caretaker;
        helperForm.IsVisible = t.caretaker;

        entryTitle.Text = t.title;
        entryDescription.Text = t.description;
        _tags.Text = string.Join(' ', t.tags);

        if (t.caretaker)
        {
            pickerFormType.SelectedIndex = 0;
            foreach (var x in t.weekDays) { int i = WeekDaysList.IndexOf(x); if (i != -1) _weekDays.SelectedItems.Add(WeekDaysList[i]); }
            foreach (var x in t.timeOfDay) { int i = TimeOfDayList.IndexOf(x); if (i != -1) _timeOfDay.SelectedItems.Add(TimeOfDayList[i]); }
            _hadPets.IsChecked = t.hadPets;
            _maxPets.Text = t.maxPets.ToString();
            _specialNeedsH.IsChecked = t.specialNeeds;
            _specialNeedsXp.Text = t.specialNeedsXP ?? string.Empty;
            foreach (var x in t.canLookafter) { int i = PetType.IndexOf(x); if (i != -1) _canLookAfter.SelectedItems.Add(PetType[i]); }
        }
        else
        {
            pickerFormType.SelectedIndex = 1;
            _totalPrice.Text = t.totalPrice.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _date0.Date = t.date0 ?? DateTime.Now;
            _date1.Date = t.date1 ?? DateTime.Now;
            foreach (var x in t.timeOfDay) { int i = TimeOfDayList.IndexOf(x); if (i != -1) _timeOfDay0.SelectedItems.Add(TimeOfDayList[i]); }
            _exactHours.IsChecked = t.exactHours;
            _time0.Time = t.timeSpan0 ?? TimeSpan.Zero;
            _time1.Time = t.timeSpan1 ?? TimeSpan.Zero;
            foreach (var nombreMascota in t.petList)
            {
                var item = UserPetList.FirstOrDefault(m => m.Nombre == nombreMascota);
                if (item != null) _petList.SelectedItems.Add(item);
            }
            _specialNeeds.IsChecked = t.specialNeeds;
            _specialNeedsDes.Text = t.specialNeedsDes ?? string.Empty;
        }
    }

    // ── Crear / guardar tarea ─────────────────────────────────────────────────
    private async void OnCreateTapped(object sender, EventArgs e)
    {
        exception = false;
        await Verification();
        if (!exception) return;

        try
        {
            tags = (_tags.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).ToList();

            // Subir foto al servidor si se seleccionó una
            string fotoUrl = string.Empty;
            if (!string.IsNullOrEmpty(_fotoRutaLocal))
                fotoUrl = await _api.SubirImagenAsync(_fotoRutaLocal, "mascotas") ?? string.Empty;

            TaskItem taskItem;

            if (pickerFormType.SelectedItem?.ToString() == "Cuidador")
            {
                weekList = _weekDays.SelectedItems.Cast<string>().ToList();
                timeList = _timeOfDay.SelectedItems.Cast<string>().ToList();
                petTypeList = _canLookAfter.SelectedItems.Cast<string>().ToList();

                taskItem = new TaskItem(
                    true,
                    fotoUrl,
                    entryTitle.Text,
                    entryDescription.Text,
                    tags,
                    weekList,
                    timeList,
                    _hadPets.IsChecked,
                    int.Parse(_maxPets.Text),
                    _specialNeedsH.IsChecked,
                    _specialNeedsXp.Text ?? string.Empty,
                    petTypeList);
            }
            else
            {
                timeList = _timeOfDay0.SelectedItems.Cast<string>().ToList();
                var selectedPets = _petList.SelectedItems.Cast<MascotaItem>().Select(m => m.Nombre).ToList();

                // InvariantCulture para que funcione tanto con coma como con punto
                string precioStr = (_totalPrice.Text ?? "0").Replace(',', '.');
                decimal precio = decimal.Parse(precioStr, System.Globalization.CultureInfo.InvariantCulture);

                taskItem = new TaskItem(
                    false,
                    fotoUrl,
                    entryTitle.Text,
                    entryDescription.Text,
                    tags,
                    precio,
                    _date0.Date,
                    _date1.Date,
                    _exactHours.IsChecked,
                    _time0.Time,
                    _time1.Time,
                    timeList,
                    _specialNeeds.IsChecked,
                    _specialNeedsDes.Text ?? string.Empty,
                    selectedPets);
            }

            await TasksRepository.Instance.SaveOrUpdateAsync(taskItem);
            await Navigation.PopAsync(animated: true);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo guardar la tarea: {ex.Message}", "OK");
        }
    }

    // ── Validación ────────────────────────────────────────────────────────────
    private async Task Verification()
    {
        if (string.IsNullOrEmpty(entryTitle.Text))
        {
            await DisplayAlertAsync("Error", "Introduzca un título a su solicitud", "OK");
            return;
        }
        if (string.IsNullOrEmpty(entryDescription.Text))
        {
            await DisplayAlertAsync("Error", "Introduzca una descripción a su solicitud", "OK");
            return;
        }
        if (pickerFormType.SelectedItem == null)
        {
            await DisplayAlertAsync("Error", "Escoja el tipo de solicitud", "OK");
            return;
        }

        if (pickerFormType.SelectedItem.ToString() == "Cuidador")
        {
            if (!int.TryParse(_maxPets.Text, out int result) || result <= 0)
            {
                await DisplayAlertAsync("Error", "Introduzca una cantidad de mascotas válida", "OK");
                return;
            }
            if (_canLookAfter.SelectedItems.Count == 0)
            {
                await DisplayAlertAsync("Error", "Introduzca tipo de mascota/s que puede cuidar", "OK");
                return;
            }
        }
        else
        {
            string precioStr = (_totalPrice.Text ?? "").Replace(',', '.');
            if (!decimal.TryParse(precioStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal price) || price < 5)
            {
                await DisplayAlertAsync("Error", "Introduzca una cantidad de dinero válida (mínimo 5€)", "OK");
                return;
            }
            if (_date0.Date < DateTime.Now.Date || _date0.Date >= DateTime.Now.AddMonths(18))
            {
                await DisplayAlertAsync("Error", "Introduzca una fecha de inicio válida", "OK");
                return;
            }
            if (_date1.Date < _date0.Date || _date1.Date >= DateTime.Now.AddMonths(18))
            {
                await DisplayAlertAsync("Error", "Introduzca una fecha de fin válida", "OK");
                return;
            }
            if (UserPetList.Count == 0)
            {
                await DisplayAlertAsync("Error", "No tienes mascotas registradas. Añade una mascota en tu perfil primero.", "OK");
                return;
            }
        }

        exception = true;
    }

    private void OnFormChanged(object sender, EventArgs e)
    {
        string formType = pickerFormType.SelectedItem?.ToString() ?? "";
        propForm.IsVisible = formType == "Propietario";
        helperForm.IsVisible = formType == "Cuidador";
    }

    private string? _fotoRutaLocal; // ruta local de la foto seleccionada

    private async void OnSeleccionarFotoTapped(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync();
            if (result is null) return;

            _fotoRutaLocal = result.FullPath;
            _photoPath.Source = ImageSource.FromFile(_fotoRutaLocal);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async void OnBuscarTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToMainPage(Navigation);

    private async void OnFavoritosTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToFavoritos(Navigation);

    private void OnReservasTapped(object sender, EventArgs e) { }

    private async void OnMensajesTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToMessages(Navigation);

    private async void OnCuentaTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToPerfil(Navigation);
}