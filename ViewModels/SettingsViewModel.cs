using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using glove_e.Services;

namespace glove_e.ViewModels;

/// <summary>ViewModel de Ajustes: contacto de emergencia, rangos y opciones.</summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IBleService _ble;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string contactName = string.Empty;

    [ObservableProperty]
    private string contactPhone = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RangoMaximoTexto))]
    private double rangoMaximo = 100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RangoCriticoTexto))]
    private double rangoCritico = 30;

    [ObservableProperty]
    private bool guardarHistorial = true;

    public string RangoMaximoTexto => $"Distancia de advertencia: {(int)RangoMaximo} cm";
    public string RangoCriticoTexto => $"Distancia crítica: {(int)RangoCritico} cm";

    public SettingsViewModel(ISettingsService settings, IBleService ble)
    {
        _settings = settings;
        _ble = ble;
        Load();
    }

    public void Load()
    {
        UserName = _settings.UserName;
        ContactName = _settings.ContactName;
        ContactPhone = _settings.ContactPhone;
        RangoMaximo = _settings.RangoMaximo;
        RangoCritico = _settings.RangoCritico;
        GuardarHistorial = _settings.GuardarHistorial;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(ContactName) || string.IsNullOrWhiteSpace(ContactPhone))
        {
            await Shell.Current.DisplayAlert("Faltan datos",
                "El contacto de emergencia necesita nombre y teléfono.", "OK");
            return;
        }
        if (RangoCritico >= RangoMaximo)
        {
            await Shell.Current.DisplayAlert("Rangos inválidos",
                "La distancia crítica debe ser menor que la de advertencia.", "OK");
            return;
        }

        _settings.UserName = UserName.Trim();
        _settings.ContactName = ContactName.Trim();
        _settings.ContactPhone = ContactPhone.Trim();
        _settings.RangoMaximo = (int)RangoMaximo;
        _settings.RangoCritico = (int)RangoCritico;
        _settings.GuardarHistorial = GuardarHistorial;

        await Shell.Current.DisplayAlert("Guardado ✅", "Configuración actualizada.", "OK");
    }

    /// <summary>Envía los rangos al ESP32 por BLE para que el guante use los mismos valores.</summary>
    [RelayCommand]
    private async Task SendConfigToGloveAsync()
    {
        if (!_ble.IsConnected)
        {
            await Shell.Current.DisplayAlert("Sin conexión",
                "Conéctate al guante primero (pestaña Inicio).", "OK");
            return;
        }

        var ok = await _ble.SendConfigAsync((int)RangoMaximo, (int)RangoCritico);
        await Shell.Current.DisplayAlert(ok ? "Enviado ✅" : "Error",
            ok ? "El guante ahora usa los nuevos rangos."
               : "No se pudo escribir la configuración.", "OK");
    }
}
