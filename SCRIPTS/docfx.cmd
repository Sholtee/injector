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

nuget install docfx.console -OutputDirectory %vendor% -Version 2.42.4

set VSINSTALLDIR=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\

call %vendor%\docfx.console.2.42.4\tools\docfx.exe %root%\SRC\docfx.json