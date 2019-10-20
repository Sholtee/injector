::
:: uploadtestresults.appveyor.cmd
::
:: Author: Denes Solti
::
@echo off

setlocal EnableDelayedExpansion

set /p root=<root
set artifacts=%root%\Artifacts

set testresults=%artifacts%\testresults.xml

if exist %testresults% (
  echo Uploading test results...
  powershell -nologo -noprofile -command "(New-Object 'System.Net.WebClient').UploadFile('https://ci.appveyor.com/api/testresults/nunit/%APPVEYOR_JOB_ID%', (Resolve-Path %testresults%))"
)

set coveragereport=%artifacts%\coverage.xml

if exist %coveragereport% (
  echo Uploading coverage report...
  
  set vendor=%root%\Vendor
  
  nuget install coveralls.io -OutputDirectory !vendor! -Version 1.4.2
  call !vendor!\coveralls.io.1.4.2\tools\coveralls.net.exe --opencover %coveragereport% -r %COVERALLS_REPO_TOKEN%
)