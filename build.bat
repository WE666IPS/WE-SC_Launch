@echo off
echo ========================================
echo    SC Launch Build Script
echo ========================================
echo.

echo [1/2] Building project...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)
echo Build completed!
echo.

echo [2/2] Creating ZIP portable package...
if not exist dist mkdir dist
powershell -Command "Compress-Archive -Path 'bin\Release\net8.0-windows\*' -DestinationPath 'dist\SC_Launch_windows.zip' -CompressionLevel Optimal"
if %errorlevel% equ 0 (
    echo ZIP created: dist\SC_Launch_windows.zip
) else (
    echo ZIP creation failed!
)

echo.
echo ========================================
echo Build completed!
echo Output: dist\SC_Launch_windows.zip
echo ========================================
echo.
pause