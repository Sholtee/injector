::
:: test.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root

dotnet test %root%\TEST\Injector.Tests.csproj --configuration:Debug --test-adapter-path:. --logger:"nunit;LogFilePath=%root%\BIN\testresults.xml"