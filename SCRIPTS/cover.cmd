::
:: cover.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root
set vendor=%root%\Vendor
set bin=%root%\BIN
set coveragexml=%bin%\coverage.xml
set reports=%root%\Coverage Report

if exist "%reports%" rmdir /Q /S "%reports%"

nuget install ReportGenerator -OutputDirectory %vendor% -Version 4.2.15
nuget install OpenCover -OutputDirectory %vendor% -Version 4.7.922

if not exist %bin% mkdir %bin%

call "%vendor%\OpenCover.4.7.922\tools\OpenCover.Console.exe" -target:"%ProgramFiles%\dotnet\dotnet.exe" -targetargs:"test %root%\TEST\Injector.Tests.csproj --configuration:Debug" -output:"%coveragexml%" -oldStyle -register:user -filter:"+[Injector*]* -[Injector.Tests]*" -excludebyattribute:"*.ExcludeFromCoverage*"
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet "%vendor%\ReportGenerator.4.2.15\tools\netcoreapp2.1\ReportGenerator.dll" "-reports:%coveragexml%" "-targetdir:%reports%"