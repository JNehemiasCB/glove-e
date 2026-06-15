using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using glove_e.Models;
using glove_e.Services;

namespace glove_e.ViewModels;

/// <summary>
/// ViewModel de la pantalla principal: conexión BLE, distancia en vivo
/// y botón de alerta de emergencia.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IBleService _ble;
    private readonly IDatabaseService _database;
    private readonly ISettingsService _settings;
    private readonly IAlertService _alert;

    // Para no saturar la BD: guardamos máximo una lectura por segundo
    private DateTime _lastSave = DateTime.MinValue;

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string connectButtonText = "Conectar al guante";

    [ObservableProperty]
    private string distanciaTexto = "--";

    [ObservableProperty]
    private string estadoTexto = "Sin conexión";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstadoBrush))]
    private Color estadoColor = Colors.Gray;

    /// <summary>Versión Brush del color, para el borde del indicador.</summary>
    public Brush EstadoBrush => new SolidColorBrush(EstadoColor);

    [ObservableProperty]
    private string saludo = "Hola";

    public MainViewModel(IBleService ble, IDatabaseService database,
                         ISettingsService settings, IAlertService alert)
    {
        _ble = ble;
        _database = database;
        _settings = settings;
        _alert = alert;

        _ble.DistanceReceived += OnDistanceReceived;
        _ble.ConnectionChanged += OnConnectionChanged;
    }

    /// <summary>Llamado desde OnAppearing de la página.</summary>
    public void RefreshSettings()
    {
        Saludo = string.IsNullOrWhiteSpace(_settings.UserName)
            ? "Hola"
            : $"Hola, {_settings.UserName}";
    }

    private void OnConnectionChanged(object? sender, bool connected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsConnected = connected;
            ConnectButtonText = connected ? "Desconectar" : "Conectar al guante";
            if (!connected)
            {
                DistanciaTexto = "--";
                EstadoTexto = "Sin conexión";
                EstadoColor = Colors.Gray;
            }
        });
    }

    private void OnDistanceReceived(object? sender, int cm)
    {
        // Clasificar usando los mismos rangos que el firmware
        string estado;
        Color color;
        if (cm <= _settings.RangoCritico)
        {
            estado = "¡CRÍTICO!";
            color = Colors.Red;
        }
        else if (cm <= _settings.RangoMaximo)
        {
            estado = "Advertencia";
            color = Colors.Orange;
        }
        else
        {
            estado = "Seguro";
            color = Colors.Green;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            DistanciaTexto = $"{cm}";
            EstadoTexto = estado;
            EstadoColor = color;
        });

        // Registrar en SQLite (máx. 1 por segundo)
        if (_settings.GuardarHistorial && (DateTime.Now - _lastSave).TotalSeconds >= 1)
        {
            _lastSave = DateTime.Now;
            _ = _database.SaveReadingAsync(new DistanceReading
            {
                Timestamp = DateTime.Now,
                Centimeters = cm,
                Estado = estado
            });
        }
    }

    [RelayCommand]
    private async Task ToggleConnectionAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (_ble.IsConnected)
            {
                await _ble.DisconnectAsync();
            }
            else
            {
                ConnectButtonText = "Buscando guante...";
                var ok = await _ble.ConnectAsync();
                if (!ok)
                {
                    ConnectButtonText = "Conectar al guante";
                    await Shell.Current.DisplayAlert("Sin conexión",
                        "No se encontró el guante 'GloveE'. Verifica que esté encendido y cerca.",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            ConnectButtonText = "Conectar al guante";
            await Shell.Current.DisplayAlert("Error de Bluetooth", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SendAlertAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.ContactPhone))
        {
            await Shell.Current.DisplayAlert("Sin contacto",
                "Configura primero un contacto de emergencia en Ajustes.", "OK");
            return;
        }

        var confirmar = await Shell.Current.DisplayAlert("🚨 Alerta de auxilio",
            $"¿Enviar mensaje de ayuda a {_settings.ContactName} ({_settings.ContactPhone})?",
            "Sí, enviar", "Cancelar");
        if (!confirmar) return;

        IsBusy = true;
        try
        {
            var alerta = await _alert.SendEmergencyAlertAsync();
            await Shell.Current.DisplayAlert("Mensaje enviado ✅",
                $"Se notificó a {alerta.ContactName} ({alerta.ContactPhone}).\n\n\"{alerta.Message}\"",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
