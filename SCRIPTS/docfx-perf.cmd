::
:: docfx-perf.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root
set dist=%root%\BenchmarkDotNet.Artifacts\perf

if exist %dist% rmdir /Q /S %dist%

set vendor=%root%\Vendor

nuget install docfx.console -OutputDirectory %vendor% -Version 2.42.4

call %vendor%\docfx.console.2.42.4\tools\docfx.exe %root%\perf\docfx.json