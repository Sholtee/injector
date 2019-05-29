::
:: docfx.cmd
::
:: Author: Denes Solti
::
@echo off

set VENDOR=%~dp0Vendor

nuget install docfx.console -OutputDirectory "%VENDOR%" -Version 2.42.4

%VENDOR%\docfx.console.2.42.4\tools\docfx docfx.json