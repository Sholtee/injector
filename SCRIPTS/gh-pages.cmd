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
@echo cloning gh-pages...

set repo_dir=%root%\gh-pages
set docs_branch=gh-pages

if exist "%repo_dir%" (
  @echo cleanup...
  rmdir /Q /S %repo_dir%
)

git clone https://github.com/sholtee/injector.git --branch %docs_branch% %repo_dir%

::--------------------------------
:: generate API docs
::--------------------------------
@echo generating API docs...

set docs_dir=%repo_dir%\doc

if exist "%docs_dir%" (
  rmdir /Q /S %docs_dir%
)

call docfx
xcopy /e /i "%root%\doc" "%repo_dir%"

::-----------------------------------------
:: generate benchmark results (if needed)
::-----------------------------------------
set bm_dir="%root%\BenchmarkDotNet.Artifacts"

if exist "%bm_dir%" (
  @echo generating benchmark docs...

  set perf_dir=%repo_dir%\perf

  if exist "%perf_dir%" (
    rmdir /Q /S %perf_dir%
  )
  
  call docfx-perf
  xcopy /e /i "%bm_dir%\perf" "%repo_dir%"
)

::--------------------------------
:: committing changes
::--------------------------------
@echo committing...

cd %repo_dir%

git checkout %docs_branch%
git add doc -A
git commit -m "docs up"
if exist "%bm_dir%" (
  git add perf -A
  git commit -m "benchmarks up"
)
git push origin %docs_branch%

cd %~dp0

rmdir /Q /S %repo_dir%