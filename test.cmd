::
:: test.cmd
::
:: Author: Denes Solti
::
@echo off

set framework=netcoreapp2.2
set bin=%~dp0BIN
set testrunner=%bin%\testrunner

call nuget install "NUnit.Console" -Framework %framework% -Version 3.10.0 -OutputDirectory "%testrunner%"
if %errorlevel% neq 0 exit /b %errorlevel%

call dotnet build Injector.sln --configuration Debug 
if %errorlevel% neq 0 exit /b %errorlevel%

call dotnet %testrunner%\NUnit.ConsoleRunner.3.10.0\tools\nunit3-console.exe "%bin%\%framework%\Injector.Test.dll"