@echo off
setlocal

echo ============================================
echo   nesplay-psp Builder
echo ============================================
echo.

set "PROJECT_DIR=%~dp0"
set "LAUNCHER_DIR=%PROJECT_DIR%launcher"
set "PUBLISH_DIR=%PROJECT_DIR%publish"

:: Create publish folder
if not exist "%PUBLISH_DIR%" mkdir "%PUBLISH_DIR%"

:: Build launcher
echo Building nesplay-psp.exe ...
dotnet publish "%LAUNCHER_DIR%\Launcher.csproj" -r win-x64 -c Release -o "%PUBLISH_DIR%" --nologo -v q

if exist "%PUBLISH_DIR%\nesplay-psp.exe" (
    echo   OK
) else (
    echo   FAILED
    exit /b 1
)

:: Copy roms/ to publish
if exist "%PROJECT_DIR%roms" (
    echo Copying roms/ ...
    xcopy "%PROJECT_DIR%roms" "%PUBLISH_DIR%\roms\" /E /I /Q /Y >nul
)

:: Copy nesplay_psp prebuilt if available
if not exist "%PUBLISH_DIR%\nesplay_psp.exe" (
    if exist "%PROJECT_DIR%nesplay_psp_prebuilt\NESPLAY_PSPWindows64.exe" (
        echo Copying nesplay_psp.exe ...
        copy "%PROJECT_DIR%nesplay_psp_prebuilt\NESPLAY_PSPWindows64.exe" "%PUBLISH_DIR%\nesplay_psp.exe" >nul
        copy "%PROJECT_DIR%nesplay_psp_prebuilt\d3dcompiler_47.dll" "%PUBLISH_DIR%\" >nul
    )
)

:: Copy assets if available
if not exist "%PUBLISH_DIR%\assets" (
    if exist "%PROJECT_DIR%nesplay_psp_prebuilt\assets" (
        echo Copying assets/ ...
        xcopy "%PROJECT_DIR%nesplay_psp_prebuilt\assets" "%PUBLISH_DIR%\assets\" /E /I /Q /Y >nul
    )
)

:: Clean up
del "%PUBLISH_DIR%\*.pdb" 2>nul

echo.
echo ============================================
echo   Build complete!
echo   Output: %PUBLISH_DIR%\nesplay-psp.exe
echo ============================================

endlocal
