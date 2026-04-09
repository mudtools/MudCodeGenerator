@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

echo ========================================
echo Mud Code Generator - Run Tests
echo ========================================
echo.

set VERBOSITY=normal
set FAILED=0

echo [1/4] Running Mud.HttpUtils.Tests...
dotnet test Tests\Mud.HttpUtils.Tests\Mud.HttpUtils.Tests.csproj --verbosity %VERBOSITY% --nologo
if errorlevel 1 (
    echo [FAILED] Mud.HttpUtils.Tests
    set FAILED=1
) else (
    echo [PASSED] Mud.HttpUtils.Tests
)
echo.

echo [2/4] Running Mud.HttpUtils.Generator.Tests...
dotnet test Tests\Mud.HttpUtils.Generator.Tests\Mud.HttpUtils.Generator.Tests.csproj --verbosity %VERBOSITY% --nologo
if errorlevel 1 (
    echo [FAILED] Mud.HttpUtils.Generator.Tests
    set FAILED=1
) else (
    echo [PASSED] Mud.HttpUtils.Generator.Tests
)
echo.

echo [3/4] Running Mud.EntityCodeGenerator.Tests...
dotnet test Tests\Mud.EntityCodeGenerator.Tests\Mud.EntityCodeGenerator.Tests.csproj --verbosity %VERBOSITY% --nologo
if errorlevel 1 (
    echo [FAILED] Mud.EntityCodeGenerator.Tests
    set FAILED=1
) else (
    echo [PASSED] Mud.EntityCodeGenerator.Tests
)
echo.

echo [4/4] Running Mud.ServiceCodeGenerator.Tests...
dotnet test Tests\Mud.ServiceCodeGenerator.Tests\Mud.ServiceCodeGenerator.Tests.csproj --verbosity %VERBOSITY% --nologo
if errorlevel 1 (
    echo [FAILED] Mud.ServiceCodeGenerator.Tests
    set FAILED=1
) else (
    echo [PASSED] Mud.ServiceCodeGenerator.Tests
)
echo.

echo ========================================
echo Test Summary
echo ========================================
if %FAILED%==1 (
    echo Some tests FAILED!
    exit /b 1
) else (
    echo All tests PASSED!
)
echo.
echo Done!
endlocal
