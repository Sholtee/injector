::
:: test.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root

set vendor=%root%\Vendor
set artifacts=%root%\Artifacts

nuget install OpenCover -OutputDirectory %vendor% -Version 4.7.922

if not exist %artifacts% mkdir %artifacts%

call "%vendor%\OpenCover.4.7.922\tools\OpenCover.Console.exe" -target:"%ProgramFiles%\dotnet\dotnet.exe" -targetargs:"test %root%\TEST\Injector.Tests.csproj --framework:netcoreapp2.2 --configuration:Debug --test-adapter-path:. --logger:nunit;LogFilePath=%artifacts%\testresults.xml" -output:"%artifacts%\coverage.xml" -oldStyle -register:user -filter:"+[Injector*]* -[Injector.Tests]*" -excludebyattribute:"*.ExcludeFromCoverage*"