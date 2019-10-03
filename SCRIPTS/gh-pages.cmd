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

set docs_dir=%repo_dir%\doc

if exist %docs_dir% (
    @echo cleanup...
    call rmdir /Q /S %docs_dir%
)

call docfx

move "%root%\doc" "%repo_dir%"

cd %repo_dir%

git checkout %docs_branch%
git add doc -A
git commit -m "docs up"
git push origin %docs_branch%

cd %~dp0

rmdir /Q /S %repo_dir%