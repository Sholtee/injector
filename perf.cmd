::
:: perf.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root

dotnet build %root%\Injector.sln -c Perf -p:NoDocfx=True 
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet "%root%\BIN\netcoreapp2.2\Injector.Perf.dll" -f * -e GitHub -a %root%\BenchmarkDotNet.Artifacts