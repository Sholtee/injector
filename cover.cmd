::
:: cover.cmd
::
:: Author: Denes Solti
::
@echo off

set vendor=%~dp0Vendor
set bin=%~dp0BIN
set coveragexml=%bin%\coverage.xml
set reports=%~dp0Coverage Report

if exist "%reports%" rmdir /Q /S "%reports%"

nuget install ReportGenerator -OutputDirectory %vendor% -Version 4.2.15
nuget install OpenCover -OutputDirectory %vendor% -Version 4.7.922

call "%vendor%\OpenCover.4.7.922\tools\OpenCover.Console.exe" -target:"%ProgramFiles%\dotnet\dotnet.exe" -targetargs:"test %~dp0TEST\Injector.Tests.csproj --configuration:Debug -p:NoDocfx=True" -output:"%coveragexml%" -oldStyle -register:user
dotnet "%vendor%\ReportGenerator.4.2.15\tools\netcoreapp2.1\ReportGenerator.dll" "-reports:%coveragexml%" "-targetdir:%reports%"