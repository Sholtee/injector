::
:: gh-pages.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root

::--------------------------------
:: prepare repo
::--------------------------------
echo cloning gh-pages...

set repo_dir=%root%\gh-pages
set docs_branch=gh-pages

if exist "%repo_dir%" (
  echo Cleanup...
  rmdir /Q /S %repo_dir%
)

git clone https://github.com/sholtee/injector.git --branch %docs_branch% %repo_dir%

set bm_artifacts=%~dp0%root%\BenchmarkDotNet.Artifacts

if not exist "%bm_artifacts%" (
  ::--------------------------------
  :: generate API docs
  ::--------------------------------
  echo Generating API docs...

  set docs_dir=%repo_dir%\doc

  if exist "%docs_dir%" (
    echo Removing old docs...
    rmdir /Q /S %docs_dir%
  )

  call docfx
  
  echo "Adding new docs (%root%\doc -> %docs_dir%)..."
  xcopy /e /i /y "%root%\doc" "%docs_dir%"
) else (
  ::-----------------------------------------
  :: generate benchmark results
  ::-----------------------------------------
  echo Generating benchmark docs...

  set perf_dir=%repo_dir%\perf

  if exist "%perf_dir%" (
    echo Removing old docs...
    rmdir /Q /S %perf_dir%
  )
  
  call docfx-perf
  
  echo "Adding new docs (%bm_artifacts%\perf -> %perf_dir%)..."
  xcopy /e /i /y  "%bm_artifacts%\perf" "%perf_dir%"
)

::--------------------------------
:: committing changes
::--------------------------------
echo Committing...

cd %repo_dir%

git checkout %docs_branch%
if not exist "%bm_artifacts%" (
  git add doc -A
  git commit -m "docs up"
) else (
  git add perf -A
  git commit -m "benchmarks up"
)
git push origin %docs_branch%

cd %~dp0

rmdir /Q /S %repo_dir%