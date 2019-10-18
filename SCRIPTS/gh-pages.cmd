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
set docs_branch=gh-pages
set repo_dir="%root%\%docs_branch%"

echo Repo dir: %repo_dir%

if exist "%repo_dir%" (
  echo Cleanup...
  rmdir /Q /S %repo_dir%
)

git clone https://github.com/sholtee/injector.git --branch "%docs_branch%" "%repo_dir%"

set bm_artifacts="%~dp0%root%\BenchmarkDotNet.Artifacts"

::--------------------------------
:: generate API docs
::--------------------------------
if not exist "%bm_artifacts%" (
  set docs_dir="%repo_dir%\doc"
  
  echo Generating API docs in "%docs_dir%"...

  if exist "%docs_dir%" (
    echo Removing old docs...
    rmdir /Q /S %docs_dir%
  )

  call docfx
  
  echo Adding new docs: "%root%\doc -> %docs_dir%"...
  xcopy /e /i /y "%root%\doc" "%docs_dir%"
  
::-----------------------------------------
:: generate benchmark results
::-----------------------------------------
) else (
  set perf_dir="%repo_dir%\perf"
  
  echo Generating benchmark docs in "%perf_dir%"...

  if exist "%perf_dir%" (
    echo Removing old docs...
    rmdir /Q /S %perf_dir%
  )
  
  call docfx-perf
  
  echo Adding new docs: "%bm_artifacts%\perf -> %perf_dir%"...
  xcopy /e /i /y  "%bm_artifacts%\perf" "%perf_dir%"
)

::--------------------------------
:: committing changes
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