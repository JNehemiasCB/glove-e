using System.Text;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace glove_e.Services;

public interface IBleService
{
    bool IsConnected { get; }

    /// <summary>Se dispara con cada distancia (cm) recibida del guante.</summary>
    event EventHandler<int>? DistanceReceived;

    /// <summary>true = conectado, false = desconectado.</summary>
    event EventHandler<bool>? ConnectionChanged;

    Task<bool> ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync();
    Task<bool> SendConfigAsync(int rangoMaximo, int rangoCritico);
}

/// <summary>
/// Comunicación BLE con el ESP32 usando Plugin.BLE.
/// Busca el dispositivo "GloveE", se suscribe a las notificaciones de
/// distancia y permite escribir la configuración de rangos.
/// </summary>
public class BleService : IBleService
{
    // Deben coincidir con el firmware (glove_e_ble.ino)
    private const string DeviceName = "GloveE";
    private static readonly Guid ServiceUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
    private static readonly Guid DistanceCharUuid = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8");
    private static readonly Guid ConfigCharUuid = Guid.Parse("5a87b4ef-3bfa-4eb2-a9c8-71d18d6b1e22");

    private readonly IBluetoothLE _ble = CrossBluetoothLE.Current;
    private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;

    private IDevice? _device;
    private ICharacteristic? _distanceChar;
    private ICharacteristic? _configChar;

    public bool IsConnected { get; private set; }

    public event EventHandler<int>? DistanceReceived;
    public event EventHandler<bool>? ConnectionChanged;

    public BleService()
    {
        _adapter.DeviceConnectionLost += (_, _) => SetConnected(false);
        _adapter.DeviceDisconnected += (_, _) => SetConnected(false);
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected)
            return true;

        if (!await EnsurePermissionsAsync())
            throw new InvalidOperationException("Permisos de Bluetooth denegados.");

        if (_ble.State != BluetoothState.On)
            throw new InvalidOperationException("El Bluetooth del teléfono está apagado.");

        // 1. Escanear hasta encontrar "GloveE"
        IDevice? found = null;
        using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        scanCts.CancelAfter(TimeSpan.FromSeconds(12));

        void OnDiscovered(object? s, DeviceEventArgs e)
        {
            if (e.Device.Name == DeviceName)
            {
                found = e.Device;
                scanCts.Cancel(); // detener el escaneo en cuanto aparece
            }
        }

        _adapter.DeviceDiscovered += OnDiscovered;
        try
        {
            await _adapter.StartScanningForDevicesAsync(cancellationToken: scanCts.Token);
        }
        catch (OperationCanceledException) { /* esperado al encontrarlo o por timeout */ }
        finally
        {
            _adapter.DeviceDiscovered -= OnDiscovered;
        }

        if (found is null)
            return false; // no apareció el guante

        // 2. Conectar y obtener servicio/características
        await _adapter.ConnectToDeviceAsync(found, cancellationToken: ct);
        _device = found;

        var service = await _device.GetServiceAsync(ServiceUuid, ct);
        if (service is null)
        {
            await DisconnectAsync();
            return false;
        }

        _distanceChar = await service.GetCharacteristicAsync(DistanceCharUuid);
        _configChar = await service.GetCharacteristicAsync(ConfigCharUuid);

        if (_distanceChar is null)
        {
            await DisconnectAsync();
            return false;
        }

        // 3. Suscribirse a las notificaciones de distancia
        _distanceChar.ValueUpdated += OnDistanceUpdated;
        await _distanceChar.StartUpdatesAsync();

        SetConnected(true);
        return true;
    }

    private void OnDistanceUpdated(object? sender, CharacteristicUpdatedEventArgs e)
    {
        var bytes = e.Characteristic.Value;
        if (bytes is null || bytes.Length == 0)
            return;

        var text = Encoding.UTF8.GetString(bytes);
        if (int.TryParse(text, out var cm))
            DistanceReceived?.Invoke(this, cm);
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_distanceChar is not null)
            {
                _distanceChar.ValueUpdated -= OnDistanceUpdated;
                try { await _distanceChar.StopUpdatesAsync(); } catch { }
            }
            if (_device is not null)
                await _adapter.DisconnectDeviceAsync(_device);
        }
        catch { /* si ya estaba desconectado, ignorar */ }
        finally
        {
            _distanceChar = null;
            _configChar = null;
            _device = null;
            SetConnected(false);
        }
    }

    /// <summary>Envía "MAX:x;CRIT:y" a la característica de configuración del ESP32.</summary>
    public async Task<bool> SendConfigAsync(int rangoMaximo, int rangoCritico)
    {
        if (!IsConnected || _configChar is null)
            return false;

        var payload = Encoding.UTF8.GetBytes($"MAX:{rangoMaximo};CRIT:{rangoCritico}");
        var result = await _configChar.WriteAsync(payload);
        return result == 0; // 0 = éxito en Plugin.BLE 3.x
    }

    private void SetConnected(bool value)
    {
        if (IsConnected == value)
            return;
        IsConnected = value;
        ConnectionChanged?.Invoke(this, value);
    }

    /// <summary>Pide los permisos de Bluetooth en tiempo de ejecución (Android).</summary>
    private static async Task<bool> EnsurePermissionsAsync()
    {
#if ANDROID
        var status = await Permissions.CheckStatusAsync<BluetoothPermissions>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<BluetoothPermissions>();
        return status == PermissionStatus.Granted;
#else
        return true;
#endif
    }
}

#if ANDROID
/// <summary>
/// Permiso compuesto para BLE:
///  - Android 12+ : BLUETOOTH_SCAN + BLUETOOTH_CONNECT
///  - Android 11- : ACCESS_FINE_LOCATION
/// </summary>
public class BluetoothPermissions : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions
    {
        get
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                return new[]
                {
                    (Android.Manifest.Permission.BluetoothScan, true),
                    (Android.Manifest.Permission.BluetoothConnect, true)
                };
            }
            return new[] { (Android.Manifest.Permission.AccessFineLocation, true) };
        }
    }
}
#endif
