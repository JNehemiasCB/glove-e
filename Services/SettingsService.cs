namespace glove_e.Services;

/// <summary>
/// Configuración persistente de la app (usa Preferences, clave-valor).
/// Aquí viven el contacto de emergencia y los rangos del guante.
/// </summary>
public interface ISettingsService
{
    bool IsConfigured { get; set; }
    string UserName { get; set; }
    string ContactName { get; set; }
    string ContactPhone { get; set; }
    int RangoMaximo { get; set; }
    int RangoCritico { get; set; }
    bool GuardarHistorial { get; set; }
}

public class SettingsService : ISettingsService
{
    public bool IsConfigured
    {
        get => Preferences.Get(nameof(IsConfigured), false);
        set => Preferences.Set(nameof(IsConfigured), value);
    }

    public string UserName
    {
        get => Preferences.Get(nameof(UserName), string.Empty);
        set => Preferences.Set(nameof(UserName), value);
    }

    public string ContactName
    {
        get => Preferences.Get(nameof(ContactName), string.Empty);
        set => Preferences.Set(nameof(ContactName), value);
    }

    public string ContactPhone
    {
        get => Preferences.Get(nameof(ContactPhone), string.Empty);
        set => Preferences.Set(nameof(ContactPhone), value);
    }

    /// <summary>Distancia (cm) donde empieza la advertencia. Igual que RANGO_MAXIMO del firmware.</summary>
    public int RangoMaximo
    {
        get => Preferences.Get(nameof(RangoMaximo), 100);
        set => Preferences.Set(nameof(RangoMaximo), value);
    }

    /// <summary>Distancia (cm) del estado crítico. Igual que RANGO_CRITICO del firmware.</summary>
    public int RangoCritico
    {
        get => Preferences.Get(nameof(RangoCritico), 30);
        set => Preferences.Set(nameof(RangoCritico), value);
    }

    public bool GuardarHistorial
    {
        get => Preferences.Get(nameof(GuardarHistorial), true);
        set => Preferences.Set(nameof(GuardarHistorial), value);
    }
}
