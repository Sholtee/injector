::
:: pack.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root

dotnet pack "%root%\SRC\Injector.csproj" -c Release