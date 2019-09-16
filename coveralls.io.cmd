::
:: coveralls.io.cmd
::
:: Author: Denes Solti
::
@echo off

set vendor=%~dp0Vendor
set bin=%~dp0BIN
set coveragexml=%bin%\coverage.xml
set reports=%~dp0Coverage Report

if exist "%reports%" rmdir /Q /S "%reports%"

nuget install coveralls.io -OutputDirectory %vendor% -Version 1.4.2
nuget install OpenCover -OutputDirectory %vendor% -Version 4.7.922

if not exist %bin% mkdir %bin%

call "%vendor%\OpenCover.4.7.922\tools\OpenCover.Console.exe" -target:"%ProgramFiles%\dotnet\dotnet.exe" -targetargs:"test %~dp0TEST\Injector.Tests.csproj --configuration:Debug -p:NoDocfx=True" -output:"%coveragexml%" -oldStyle -register:user -filter:"+[Injector*]* -[Injector.Tests]*"
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet "%vendor%\coveralls.io.1.4.2\tools\coveralls.net.exe --opencover %coveragexml% -r %COVERALLS_REPO_TOKEN%