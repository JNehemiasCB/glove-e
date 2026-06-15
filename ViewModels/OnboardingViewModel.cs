using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using glove_e.Services;

namespace glove_e.ViewModels;

/// <summary>
/// ViewModel de la configuración inicial (primera vez que se abre la app).
/// Pide el nombre del usuario y su contacto de emergencia.
/// </summary>
public partial class OnboardingViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string contactName = string.Empty;

    [ObservableProperty]
    private string contactPhone = string.Empty;

    public OnboardingViewModel(ISettingsService settings)
    {
        _settings = settings;
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName))
        {
            await Shell.Current.DisplayAlert("Falta tu nombre",
                "Escribe tu nombre para personalizar la app.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(ContactName) || ContactPhone.Trim().Length < 8)
        {
            await Shell.Current.DisplayAlert("Contacto de emergencia",
                "Escribe el nombre y un teléfono válido (mínimo 8 dígitos) de tu contacto de emergencia.",
                "OK");
            return;
        }

        _settings.UserName = UserName.Trim();
        _settings.ContactName = ContactName.Trim();
        _settings.ContactPhone = ContactPhone.Trim();
        _settings.IsConfigured = true;

        // Ir a la pantalla principal (las pestañas)
        await Shell.Current.GoToAsync("//inicio");
    }
}
