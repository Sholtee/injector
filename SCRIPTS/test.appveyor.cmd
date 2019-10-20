::
:: test.appveyor.cmd
::
:: Author: Denes Solti
::
@echo off

if /i "%APPVEYOR_REPO_TAG_NAME%" == "perf" (
  echo Running performance tests...
  call perf
) else (
  echo Running regular tests...
  call testNcover
)