::
:: pack.cmd
::
:: Author: Denes Solti
::
@echo off

call dotnet clean "Injector.sln"
call rmdir /Q /S "BIN"
call dotnet build "SRC\Injector.csproj" /p:configuration=Release;NoDocfx=True
call nuget pack "Injector.nuspec" -OutputDirectory "BIN"

