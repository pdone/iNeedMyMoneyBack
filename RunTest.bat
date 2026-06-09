@echo off
setlocal

for /f "usebackq tokens=*" %%i in (`powershell -Command "Get-ChildItem -Path 'C:\Program Files*\Microsoft Visual Studio' -Recurse -Filter 'MSBuild.exe' -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match 'Current' } | Select-Object -First 1 -ExpandProperty FullName"`) do set MSBUILD=%%i

for /f "usebackq tokens=*" %%i in (`powershell -Command "Get-ChildItem -Path 'C:\Program Files*\Microsoft Visual Studio' -Recurse -Filter 'vstest.console.exe' -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName"`) do set VSTEST=%%i

if not defined MSBUILD (
    echo MSBuild not found.
    exit /b 1
)

if not defined VSTEST (
    echo vstest.console not found.
    exit /b 1
)

echo Building Tests (Debug)...
"%MSBUILD%" src\iNeedMyMoneyBack.Tests\iNeedMyMoneyBack.Tests.csproj -p:Configuration=Debug -v:minimal
if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo Running tests...
"%VSTEST%" src\iNeedMyMoneyBack.Tests\bin\Debug\iNeedMyMoneyBack.Tests.dll
if errorlevel 1 (
    echo Tests failed.
    exit /b 1
)
echo Tests passed.
