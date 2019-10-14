::
:: perf.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root

dotnet build %root%\Injector.sln -c Perf
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet "%root%\BIN\netcoreapp3.0\Injector.Perf.dll" -f * -e GitHub -a %root%\BenchmarkDotNet.Artifacts