::
:: gh-pages.cmd
::
:: Author: Denes Solti
::
@echo off

set /p root=<root
set repo_dir=%root%\gh-pages

if exist %repo_dir% (
    @echo cleanup...
    call rmdir /Q /S %repo_dir%
)

git clone https://github.com/sholtee/injector.git --branch gh-pages %repo_dir%

set dox=%repo_dir%\doc

if exist %dox% (
    @echo cleanup...
    call rmdir /Q /S %dox%
)

call docfx

move "%root%\doc" "%repo_dir%"

set git_dir=%repo_dir%\.git

git --git-dir %git_dir% add . -A
git --git-dir %git_dir% commit -m "docs up"
git --git-dir %git_dir% push origin gh-pages