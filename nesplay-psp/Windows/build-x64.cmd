@echo off
rem // Copyright (c) 2012- NESPLAY_PSP Project.

rem // This program is free software: you can redistribute it and/or modify
rem // it under the terms of the GNU General Public License as published by
rem // the Free Software Foundation, version 2.0 or later versions.

rem // This program is distributed in the hope that it will be useful,
rem // but WITHOUT ANY WARRANTY; without even the implied warranty of
rem // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
rem // GNU General Public License 2.0 for more details.

rem // A copy of the GPL 2.0 should have been included with the program.
rem // If not, see http://www.gnu.org/licenses/

rem // Official git repository and contact information can be found at
rem // https://github.com/hrydgard/nesplay_psp and http://www.nesplay_psp.org/.

if not defined vs140comntools (
  echo "Visual Studio 2015 doesn't appear to be installed properly. Quitting."
  goto quit
) else (
    call "%vs140comntools%\vsvars32.bat" x86_amd64
)

set NESPLAY_PSP_ROOT=%CD%\..
set RELEASE_DIR=%CD%\bin-release
set RELEASE_X64=NESPLAY_PSPWindows64.exe
set DEBUG_X64=NESPLAY_PSPDebug64.exe

set RLS_NESPLAY_PSP=/m /p:Configuration=Release;Platform=x64
set DBG_NESPLAY_PSP=/m /p:Configuration=Debug;Platform=x64

call msbuild NESPLAY_PSP.sln /t:Clean %RLS_NESPLAY_PSP%
call msbuild NESPLAY_PSP.sln /t:Build %RLS_NESPLAY_PSP%
if not exist "%NESPLAY_PSP_ROOT%\%RELEASEX64%" (
    echo Release build failed.
    goto Quit
)
call msbuild NESPLAY_PSP.sln /t:Clean %DBG_NESPLAY_PSP% /m
call msbuild NESPLAY_PSP.sln /t:Build %DBG_NESPLAY_PSP% /m
if not exist "%NESPLAY_PSP_ROOT%\%DEBUGX64%" (
    echo Debug build failed.
    goto Quit
)

if not exist "%RELEASE_DIR%\\." (
  mkdir %RELEASE_DIR%
) else (
  rmdir /S /Q %RELEASE_DIR%
  mkdir %RELEASE_DIR%
)

cd /d %RELEASE_DIR%

xcopy "%NESPLAY_PSP_ROOT%\assets" ".\assets\*" /S
xcopy "%NESPLAY_PSP_ROOT%\flash0" ".\flash0\*" /S
xcopy "%NESPLAY_PSP_ROOT%\lang" ".\lang\*" /S

copy %NESPLAY_PSP_ROOT%\LICENSE.txt /Y
copy %NESPLAY_PSP_ROOT%\README.md /Y

copy %NESPLAY_PSP_ROOT%\%DEBUG_X64% /Y

copy %NESPLAY_PSP_ROOT%\%RELEASE_X64% /Y

:Quit
pause
