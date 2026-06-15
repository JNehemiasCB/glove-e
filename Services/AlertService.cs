using glove_e.Models;

namespace glove_e.Services;

public interface IAlertService
{
    /// <summary>Simula el envío de un SMS al contacto de emergencia y lo registra en la BD.</summary>
    Task<AlertEvent> SendEmergencyAlertAsync();
}

/// <summary>
/// Servicio de alerta de auxilio. En esta versión el envío es SIMULADO:
/// no manda un SMS real, solo lo registra como si se hubiera enviado.
/// Para hacerlo real bastaría reemplazar el Task.Delay por un proveedor
/// de SMS (Twilio, SMS nativo, etc.).
/// </summary>
public class AlertService : IAlertService
{
    private readonly ISettingsService _settings;
    private readonly IDatabaseService _database;

    public AlertService(ISettingsService settings, IDatabaseService database)
    {
        _settings = settings;
        _database = database;
    }

    public async Task<AlertEvent> SendEmergencyAlertAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.ContactPhone))
            throw new InvalidOperationException("No hay contacto de emergencia configurado.");

        var alert = new AlertEvent
        {
            Timestamp = DateTime.Now,
            ContactName = _settings.ContactName,
            ContactPhone = _settings.ContactPhone,
            Message = $"🚨 {_settings.UserName} necesita ayuda. " +
                      $"Mensaje enviado desde la app Glove-E."
        };

        // --- SIMULACIÓN del envío del SMS ---
        await Task.Delay(1500);

        await _database.SaveAlertAsync(alert);
        return alert;
    }
}
