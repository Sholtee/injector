::
:: test.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root

set vendor=%root%\Vendor
set bin=%root%\BIN

nuget install OpenCover -OutputDirectory %vendor% -Version 4.7.922

if not exist %bin% mkdir %bin%

call "%vendor%\OpenCover.4.7.922\tools\OpenCover.Console.exe" -target:"%ProgramFiles%\dotnet\dotnet.exe" -targetargs:"test %root%\TEST\Injector.Tests.csproj --framework:netcoreapp2.2 --configuration:Debug --test-adapter-path:. --logger:nunit;LogFilePath=%bin%\testresults.xml" -output:"%bin%\coverage.xml" -oldStyle -register:user -filter:"+[Injector*]* -[Injector.Tests]*" -excludebyattribute:"*.ExcludeFromCoverage*"