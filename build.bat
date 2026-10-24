@echo off
setlocal
cd /d "%~dp0"

for /f "tokens=1 delims=." %%a in ('dotnet --version 2^>nul') do set MAJOR=%%a
if "%MAJOR%"=="" (
  echo .NET SDK not found. Install it from https://dotnet.microsoft.com/download
  echo ^(Only needed to BUILD. The finished exe runs without .NET installed.^)
  pause & exit /b 1
)
if %MAJOR% LSS 8 (
  echo .NET SDK 8 or newer is required. Found major version %MAJOR%.
  pause & exit /b 1
)
echo Using .NET SDK major version %MAJOR%

if exist dist rmdir /s /q dist
if exist obj rmdir /s /q obj
if exist bin rmdir /s /q bin

echo Building self-contained ScreenDimmer.exe ^(first run downloads packages^)...
dotnet publish ScreenDimmer.csproj -c Release -r win-x64 --self-contained true ^
  -p:TargetFramework=net%MAJOR%.0-windows -p:PublishSingleFile=true ^
  -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:DebugType=none -p:DebugSymbols=false -o dist
if errorlevel 1 (
  echo.
  echo Build failed. See the error above.
  pause & exit /b 1
)

echo.
echo Done: %~dp0dist\ScreenDimmer.exe  ^(self-contained, no .NET install needed^)
pause
