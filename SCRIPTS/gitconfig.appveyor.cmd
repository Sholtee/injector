::
:: gitconfig.appveyor.cmd
::
:: Author: Denes Solti
::
@echo off

git config --global credential.helper store
powershell -nologo -noprofile -command "Add-Content '%USERPROFILE%\.git-credentials' 'https://%GITHUB_REPO_TOKEN%:x-oauth-basic@github.com'"
git config --global user.email %GITHUB_EMAIL%
git config --global user.name "[Bot]"