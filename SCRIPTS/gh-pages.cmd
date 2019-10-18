::
:: gh-pages.cmd
::
:: Author: Denes Solti
::
@echo off

setlocal EnableDelayedExpansion

set /p root=<root

::--------------------------------
:: Prepare repo
::--------------------------------

set docs_branch=gh-pages
set repo_dir=%root%\%docs_branch%

echo Repo dir: "%repo_dir%"

if exist "%repo_dir%" (
  echo Cleanup...
  rmdir /Q /S "%repo_dir%"
)

git clone https://github.com/sholtee/injector.git --branch "%docs_branch%" "%repo_dir%"

set bm_artifacts=%~dp0%root%\BenchmarkDotNet.Artifacts

::--------------------------------
:: Generating docs
::--------------------------------

if not exist "%bm_artifacts%" (
  set docs_dir=%repo_dir%\doc
  
  echo API docs folder: "!docs_dir!"

  if exist "!docs_dir!" (
    echo Removing old docs...
    rmdir /Q /S "!docs_dir!"
  )

  echo Building API docs...
  call docfx
  
  echo Moving docs...  
  move "%root%\doc" "%repo_dir%"
) else (
  set perf_dir=%repo_dir%\perf
  
  echo Benchmark docs folder: "!perf_dir!"

  if exist "!perf_dir!" (
    echo Removing old docs...
    rmdir /Q /S "!perf_dir!"
  )
  
  echo Building benchmark docs...
  call docfx-perf
  
  echo Moving docs... 
  move "%bm_artifacts%\perf" "%repo_dir%"
)

::--------------------------------
:: Committing changes
::--------------------------------

cd %repo_dir%

git checkout %docs_branch%

if not exist "%bm_artifacts%" (
  echo Committing API docs...
  
  git add doc -A
  git commit -m "docs up"
) else (
  echo Committing benchmark docs...
  
  git add perf -A
  git commit -m "benchmarks up"
)
git push origin %docs_branch%

cd %~dp0

rmdir /Q /S %repo_dir%