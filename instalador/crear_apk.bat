@echo off
REM ============================================
REM  Glove-E : Generador de APK instalable
REM  Doble clic en este archivo para crear el APK
REM ============================================
cd /d "%~dp0.."

echo.
echo Compilando Glove-E en modo Release (puede tardar varios minutos)...
echo.

dotnet publish -f net8.0-android -c Release

if errorlevel 1 (
    echo.
    echo *** ERROR al compilar. Revisa los mensajes de arriba. ***
    pause
    exit /b 1
)

copy /Y "bin\Release\net8.0-android\publish\com.companyname.glovee-Signed.apk" "instalador\glove-e.apk" >nul

if errorlevel 1 (
    echo.
    echo No se encontro el APK esperado. Busca el archivo *-Signed.apk en:
    echo   bin\Release\net8.0-android\publish\
    pause
    exit /b 1
)

echo.
echo ============================================
echo  LISTO: instalador\glove-e.apk
echo ============================================
pause
