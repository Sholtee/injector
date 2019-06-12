::
:: pack.cmd
::
:: Author: Denes Solti
::
@echo off

set verFile=".\version"

call powershell -nologo -noprofile -command "$xml = [Xml] (Get-Content .\SRC\Injector.csproj); $xml.Project.PropertyGroup.Version" > %verFile%
if %errorlevel% neq 0 exit /b %errorlevel%

set /p ver=<%verFile%
del %verFile%

if exist "BIN" (
	@echo cleanup...
    call rmdir /Q /S ".\BIN"
)

setlocal enabledelayedexpansion
for /f %%i in (targets) do (
    @echo: 
    @echo build against: %%i

    call dotnet build "SRC\Injector.csproj" -c Release -p:TargetFramework=%%i;NoDocfx=True
    if !errorlevel! neq 0 exit /b !errorlevel!
)

call nuget pack "Injector.nuspec" -OutputDirectory "BIN" -Version %ver%

