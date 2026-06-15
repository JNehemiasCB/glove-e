# Glove-E · App móvil + ESP32 (BLE) con MVVM

Guía del proyecto: app .NET MAUI que se conecta por Bluetooth LE al guante (ESP32 + HC-SR04), registra distancias en SQLite y simula el envío de un mensaje de auxilio al contacto de emergencia.

---

## 1. Arquitectura MVVM

```
┌─────────────── VIEW (XAML) ───────────────┐
│ OnboardingPage · MainPage                 │
│ HistoryPage · SettingsPage                │
└──────────────┬────────────────────────────┘
               │ Data Binding + Commands
┌──────────────▼──────────────── VIEWMODEL ─┐
│ OnboardingViewModel · MainViewModel       │
│ HistoryViewModel · SettingsViewModel      │
│ (CommunityToolkit.Mvvm)                   │
└──────────────┬────────────────────────────┘
               │ Inyección de dependencias
┌──────────────▼──────────── MODEL/SERVICES ┐
│ BleService      → Bluetooth LE (Plugin.BLE)│
│ DatabaseService → SQLite (historial)       │
│ SettingsService → Preferences (config)     │
│ AlertService    → alerta simulada          │
│ Models: DistanceReading, AlertEvent        │
└────────────────────────────────────────────┘
```

Reglas que se respetan: las **Views no tienen lógica** (solo bindings), los **ViewModels no conocen las Views** (usan `Shell.Current` solo para navegación/diálogos) y los **Services se inyectan por interfaz** (fácil de testear/reemplazar). Todo se registra en `MauiProgram.cs`.

## 2. Estructura de archivos

| Archivo | Rol |
|---|---|
| `firmware/glove_e_ble/glove_e_ble.ino` | Firmware ESP32 con BLE (reemplaza tu sketch) |
| `Models/DistanceReading.cs`, `Models/AlertEvent.cs` | Entidades SQLite |
| `Services/BleService.cs` | Escaneo, conexión, notificaciones y escritura BLE |
| `Services/DatabaseService.cs` | Guardar/leer historial (SQLite) |
| `Services/SettingsService.cs` | Contacto de emergencia y rangos (Preferences) |
| `Services/AlertService.cs` | Simula el SMS de auxilio y lo registra |
| `ViewModels/*.cs` | Lógica de presentación |
| `Views/*.xaml` + `MainPage.xaml` | Interfaz |
| `AppShell.xaml` | Navegación: onboarding + 3 pestañas |
| `MauiProgram.cs` | Registro de DI |

## 3. Cómo funciona el BLE

El ESP32 se anuncia como **`GloveE`** con un servicio GATT:

| Característica | UUID | Uso |
|---|---|---|
| Distancia | `beb5483e-36e1-4688-b7f5-ea07361b26a8` | Notify: envía la distancia (texto, cm) cada 300 ms |
| Configuración | `5a87b4ef-3bfa-4eb2-a9c8-71d18d6b1e22` | Write: la app envía `MAX:100;CRIT:30` para cambiar rangos |

Servicio: `4fafc201-1fb5-459e-8fcc-c5c9c331914b`. Si cambias un UUID, cámbialo en ambos lados (`glove_e_ble.ino` y `BleService.cs`).

## 4. Pasos para ponerlo en marcha

### Paso 1 — Firmware
1. Abre `firmware/glove_e_ble/glove_e_ble.ino` en Arduino IDE.
2. Placa: tu ESP32 de siempre. La librería BLE viene incluida en el core de ESP32.
3. Sube el sketch. En el Monitor Serie debe aparecer `BLE listo. Anunciando como 'GloveE'`.
4. El motor y buzzer funcionan igual que antes, con o sin app conectada.

### Paso 2 — Restaurar paquetes de la app
Abre la solución en Visual Studio 2022; los NuGet se restauran solos (`CommunityToolkit.Mvvm`, `Plugin.BLE`, `sqlite-net-pcl`, `SQLitePCLRaw.bundle_green`). Si no: clic derecho en la solución → *Restore NuGet Packages*.

### Paso 3 — Ejecutar en Android
1. Conecta tu teléfono con depuración USB activada (BLE no funciona en el emulador).
2. Selecciona el destino Android y ejecuta (F5).
3. La primera vez, la app pide los permisos de Bluetooth: acéptalos.

### Paso 4 — Flujo de uso
1. **Primera vez:** pantalla de bienvenida → nombre + contacto de emergencia → *Comenzar* (esto solo aparece una vez; queda guardado en Preferences).
2. **Inicio:** *Conectar al guante* → escanea ~12 s, se conecta y muestra la distancia en vivo con color (verde/naranja/rojo, mismos rangos que el firmware).
3. **🚨 PEDIR AYUDA:** confirma y *simula* el envío del mensaje al contacto (1.5 s de "envío" + registro en SQLite). No manda un SMS real.
4. **Historial:** lecturas (máx. 1 por segundo para no saturar) y alertas enviadas. Se puede borrar.
5. **Ajustes:** editar contacto, activar/desactivar historial, y mover los rangos con sliders. *Enviar rangos al guante* los escribe por BLE y el ESP32 los aplica al instante.

## 5. Detalles técnicos importantes

- **Permisos Android:** `BLUETOOTH_SCAN/CONNECT` (Android 12+) y `ACCESS_FINE_LOCATION` (Android 11-) ya están en el `AndroidManifest.xml`; la clase `BluetoothPermissions` (en `BleService.cs`) los pide en tiempo de ejecución.
- **Hilos:** las notificaciones BLE llegan en un hilo secundario; el ViewModel actualiza la UI con `MainThread.BeginInvokeOnMainThread`.
- **Reconexión:** si el guante se apaga, el evento `DeviceConnectionLost` regresa la UI a "Sin conexión"; el ESP32 vuelve a anunciarse solo.
- **SQLite:** la BD vive en `AppDataDirectory/glove_e.db3`; las tablas se crean automáticamente.
- **Para hacer el SMS real** más adelante: en `AlertService.cs` reemplaza el `Task.Delay(1500)` por `Sms.Default.ComposeAsync(...)` (nativo, abre la app de mensajes) o un proveedor como Twilio.

## 6. Para probar sin el guante

Si quieres probar la app sin hardware, puedes usar la app **nRF Connect** (en otro teléfono) creando un GATT server con los mismos UUIDs y enviando valores como texto, o temporalmente hacer que `BleService` dispare `DistanceReceived` con valores aleatorios.
