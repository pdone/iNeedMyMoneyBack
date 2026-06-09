@echo off
setlocal

set CONFIG=Debug
if /i "%~1"=="Release" set CONFIG=Release

for /f "usebackq tokens=*" %%i in (`powershell -Command "Get-ChildItem -Path 'C:\Program Files*\Microsoft Visual Studio' -Recurse -Filter 'MSBuild.exe' -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match 'Current' } | Select-Object -First 1 -ExpandProperty FullName"`) do set MSBUILD=%%i

if not defined MSBUILD (
    echo MSBuild not found.
    exit /b 1
)

echo Building iNeedMyMoneyBack (%CONFIG%)...
"%MSBUILD%" src\iNeedMyMoneyBack.sln -p:Configuration=%CONFIG% -v:minimal
if errorlevel 1 (
    echo Build failed.
    exit /b 1
)
echo Build succeeded.
