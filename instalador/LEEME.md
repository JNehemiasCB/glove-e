# Instalador de Glove-E (APK)

## Cómo generar el APK

Doble clic en **`crear_apk.bat`**. Compila la app en Release y deja el archivo
**`glove-e.apk`** en esta misma carpeta.

(Equivalente manual: `dotnet publish -f net8.0-android -c Release` desde la
raíz del proyecto; el APK firmado queda en `bin\Release\net8.0-android\publish\`.)

## Cómo instalarlo en cualquier celular

1. Pasa `glove-e.apk` al celular (WhatsApp, Drive, USB, etc.).
2. Abre el archivo en el celular.
3. Android avisará "instalar apps de origen desconocido": permite la instalación
   para esa app (Chrome, WhatsApp o el explorador de archivos, según de dónde lo abras).
4. Instalar → Abrir. Acepta los permisos de Bluetooth al iniciar.

## Notas

- El APK va firmado con la firma de depuración: sirve perfectamente para
  instalarlo y compartirlo directo, pero no para subirlo a Google Play.
- Requiere Android 5.0+ y Bluetooth LE (cualquier celular moderno).
- Si actualizas el código, vuelve a ejecutar `crear_apk.bat` para regenerar el APK.
