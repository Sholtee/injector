::
:: test.cmd
::
:: Author: Denes Solti
::
@echo off

set vendor=%~dp0Vendor
set reportfolder=Coverage Report

set binreport=%~dp0BIN\%reportfolder%
if exist "%binreport%" rmdir /Q /S "%binreport%"

nuget install ReportGenerator  -OutputDirectory %vendor% -Version 4.2.15
dotnet test "%~dp0TEST\Injector.Tests.csproj" --configuration:Debug -p:NoDocfx=True --results-directory:"%binreport%" --collect:"Code Coverage"
dotnet "%vendor%\ReportGenerator.4.2.15\tools\netcoreapp2.1\ReportGenerator.dll" "-reports:%binreport%\*\*.coverage" "-targetdir:%~dp0%reportfolder%"