::
:: perf.cmd
::
:: Author: Denes Solti
::
@echo off

call dotnet build Injector.sln -c Perf -p:NoDocfx=True 
if %errorlevel% neq 0 exit /b %errorlevel%

call dotnet "%~dp0BIN\netcoreapp2.2\Injector.Perf.dll" -f *