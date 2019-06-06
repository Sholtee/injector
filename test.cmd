::
:: test.cmd
::
:: Author: Denes Solti
::
@echo off

dotnet test "%~dp0TEST\Injector.Tests.csproj" -c Debug -p:NoDocfx=True