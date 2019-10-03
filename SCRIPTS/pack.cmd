::
:: pack.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root
set csproj="%root%\SRC\Injector.csproj"
set verFile="%root%\version"

powershell -nologo -noprofile -command "$xml = [Xml] (Get-Content %csproj%); $xml.Project.PropertyGroup.Version" > %verFile%
if %errorlevel% neq 0 exit /b %errorlevel%

set /p ver=<%verFile%
del %verFile%

set bin="%root%\BIN"

if exist %bin% (
    @echo cleanup...
    call rmdir /Q /S %bin%
)

setlocal enabledelayedexpansion
for /f %%i in (%root%\targets) do (
    @echo: 
    @echo build against: %%i

    dotnet build %csproj% -c Release -p:TargetFramework=%%i
    if !errorlevel! neq 0 exit /b !errorlevel!
)

nuget pack %root%\Injector.nuspec -OutputDirectory %bin% -Version %ver%