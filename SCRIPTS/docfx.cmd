::
:: docfx.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root
set doc=%root%\doc

if exist %doc% rmdir /Q /S %doc%

set vendor=%root%\Vendor

nuget install docfx.console -OutputDirectory %vendor% -Version 2.46.0

call %vendor%\docfx.console.2.46.0\tools\docfx.exe %1