@echo off
set LOGFILE=nesplay_psplog.txt

del "%LOGFILE%" 2> NUL
if exist NESPLAY_PSPWindows64.exe (
    NESPLAY_PSPWindows64.exe --log="%LOGFILE%"
    goto exit
)
if exist NESPLAY_PSPWindows.exe (
    NESPLAY_PSPWindows.exe --log="%LOGFILE%"
    goto exit
)

echo Unable to find NESPLAY_PSPWindows.exe.
pause

:exit