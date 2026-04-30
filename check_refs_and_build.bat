@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

echo Project folder: %CD%

echo.
echo [1/3] Checking files...
if not exist SpotlightEffect.csproj goto missing_project
if not exist SpotlightVideoEffect.cs goto missing_videoeffect
if not exist SpotlightVideoEffectProcessor.cs goto missing_processor
if not exist Directory.Build.props goto missing_props

echo.
echo [2/3] Current .cs files:
dir /b *.cs

echo.
echo [3/3] Building...
dotnet build SpotlightEffect.csproj -c Release
pause
exit /b %ERRORLEVEL%

:missing_project
echo [ERROR] SpotlightEffect.csproj was not found.
pause
exit /b 1
:missing_videoeffect
echo [ERROR] SpotlightVideoEffect.cs was not found.
pause
exit /b 1
:missing_processor
echo [ERROR] SpotlightVideoEffectProcessor.cs was not found.
pause
exit /b 1
:missing_props
echo [ERROR] Directory.Build.props was not found.
pause
exit /b 1
