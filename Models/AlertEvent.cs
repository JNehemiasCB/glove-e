using SQLite;

namespace glove_e.Models;

/// <summary>Registro de una alerta de auxilio enviada (simulada) al contacto de emergencia.</summary>
[Table("alerts")]
public class AlertEvent
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public string ContactName { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
