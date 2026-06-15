using SQLite;

namespace glove_e.Models;

/// <summary>Una lectura de distancia recibida del guante por BLE.</summary>
[Table("readings")]
public class DistanceReading
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    /// <summary>Distancia en centímetros.</summary>
    public int Centimeters { get; set; }

    /// <summary>Seguro / Advertencia / Crítico.</summary>
    public string Estado { get; set; } = string.Empty;
}
