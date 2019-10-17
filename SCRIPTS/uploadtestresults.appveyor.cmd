::
:: uploadtestresults.appveyor.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root
set testresults=%root%\BIN\testresults.xml

if exist %testresults% (
  echo Uploading test results...
  powershell -nologo -noprofile -command "(New-Object 'System.Net.WebClient').UploadFile('https://ci.appveyor.com/api/testresults/nunit/%APPVEYOR_JOB_ID%', (Resolve-Path %testresults%))"
)