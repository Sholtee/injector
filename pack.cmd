::
:: pack.cmd
::
:: Author: Denes Solti
::
@echo off

call dotnet clean "Injector.sln"
call rmdir /Q /S "BIN"
call dotnet build "SRC\Injector.csproj" --configuration Release
call nuget pack "Injector.nuspec" -OutputDirectory "BIN"
