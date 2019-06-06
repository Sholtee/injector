::
:: test.cmd
::
:: Author: Denes Solti
::
@echo off

dotnet test "%~dp0TEST\Injector.Tests.csproj" /p:configuration=Debug;NoDocfx=True