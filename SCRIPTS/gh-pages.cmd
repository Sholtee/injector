::
:: gh-pages.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root
set repo_dir=%root%\gh-pages
set docs_branch=gh-pages

if exist %repo_dir% (
    @echo cleanup...
    call rmdir /Q /S %repo_dir%
)

git clone https://github.com/sholtee/injector.git --branch %docs_branch% %repo_dir%

@echo deleting the old content...

set docs_dir=%repo_dir%\doc

if exist %docs_dir% (
    call rmdir /Q /S %docs_dir%
)

set perf_dir=%repo_dir%\perf

if exist %perf_dir% (
    call rmdir /Q /S %perf_dir%
)

@echo calling DocFx...

call docfx
call docfx-perf

@echo moving the new content...

move "%root%\doc" "%repo_dir%"
move "%root%\BenchmarkDotNet.Artifacts\perf" "%repo_dir%"

@echo committing changes...

cd %repo_dir%

git checkout %docs_branch%
git add doc -A
git commit -m "docs up"
git add perf -A
git commit -m "benchmarks up"
git push origin %docs_branch%

cd %~dp0

rmdir /Q /S %repo_dir%