::
:: pack.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root
set tmpfile="%root%\currentbranch"
git rev-parse --abbrev-ref HEAD>%tmpfile%
set /p currentbranch=<%tmpfile%
del %tmpfile%

dotnet pack "%root%\SRC\Injector.csproj" -c Release /p:CurrentBranch=%currentbranch%