@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

echo ========================================
echo Mud Code Generator - NuGet Package Build
echo ========================================
echo.

set CONFIGURATION=Release
set OUTPUT_DIR=artifacts\packages

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo [1/4] Building Mud.HttpUtils...
dotnet build Core\Mud.HttpUtils\Mud.HttpUtils.csproj -c %CONFIGURATION% --nologo -v q
if errorlevel 1 (
    echo [ERROR] Failed to build Mud.HttpUtils
    exit /b 1
)

echo [2/4] Building Mud.HttpUtils.Generator...
dotnet build Core\Mud.HttpUtils.Generator\Mud.HttpUtils.Generator.csproj -c %CONFIGURATION% --nologo -v q
if errorlevel 1 (
    echo [ERROR] Failed to build Mud.HttpUtils.Generator
    exit /b 1
)

echo [3/4] Building Mud.EntityCodeGenerator...
dotnet build Core\Mud.EntityCodeGenerator\Mud.EntityCodeGenerator.csproj -c %CONFIGURATION% --nologo -v q
if errorlevel 1 (
    echo [ERROR] Failed to build Mud.EntityCodeGenerator
    exit /b 1
)

echo [4/4] Building Mud.ServiceCodeGenerator...
dotnet build Core\Mud.ServiceCodeGenerator\Mud.ServiceCodeGenerator.csproj -c %CONFIGURATION% --nologo -v q
if errorlevel 1 (
    echo [ERROR] Failed to build Mud.ServiceCodeGenerator
    exit /b 1
)

echo.
echo ========================================
echo Creating NuGet Packages...
echo ========================================
echo.

echo [1/4] Packing Mud.HttpUtils...
dotnet pack Core\Mud.HttpUtils\Mud.HttpUtils.csproj -c %CONFIGURATION% --output %OUTPUT_DIR% --nologo -v q
if errorlevel 1 (
    echo [ERROR] Failed to pack Mud.HttpUtils
    exit /b 1
)

echo [2/4] Packing Mud.HttpUtils.Generator...
dotnet pack Core\Mud.HttpUtils.Generator\Mud.HttpUtils.Generator.csproj -c %CONFIGURATION% --output %OUTPUT_DIR% --nologo -v q
if errorlevel 1 (
    echo [ERROR] Failed to pack Mud.HttpUtils.Generator
    exit /b 1
)

echo [3/4] Packing Mud.EntityCodeGenerator...
dotnet pack Core\Mud.EntityCodeGenerator\Mud.EntityCodeGenerator.csproj -c %CONFIGURATION% --output %OUTPUT_DIR% --nologo -v q
if errorlevel 1 (
    echo [ERROR] Failed to pack Mud.EntityCodeGenerator
    exit /b 1
)

echo [4/4] Packing Mud.ServiceCodeGenerator...
dotnet pack Core\Mud.ServiceCodeGenerator\Mud.ServiceCodeGenerator.csproj -c %CONFIGURATION% --output %OUTPUT_DIR% --nologo -v q
if errorlevel 1 (
    echo [ERROR] Failed to pack Mud.ServiceCodeGenerator
    exit /b 1
)

echo.
echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Packages saved to: %OUTPUT_DIR%
echo.
dir /b "%OUTPUT_DIR%\*.nupkg" 2>nul
echo.
echo Done!
endlocal
