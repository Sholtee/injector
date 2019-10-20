::
:: deploy.appveyor.cmd
::
:: Author: Denes Solti
::
@echo off

setlocal EnableDelayedExpansion

set /p root=<root
set tmpfile="%root%\version"
powershell -nologo -noprofile -command "'%APPVEYOR_REPO_TAG_NAME%' -match '^v\d+.\d+.\d+[-\w]*$'">%tmpfile%
set /p deploying=<%tmpfile%
del %tmpfile%

if /i %deploying% == true (
  set bin=%root%\BIN

  if exist "!bin!" (
    echo Cleaning...
    rmdir /Q /S "!bin!"
  )

  echo Deploying...
  call pack
)