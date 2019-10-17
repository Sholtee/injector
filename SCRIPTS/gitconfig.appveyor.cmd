::
:: gitconfig.appveyor.cmd
::
:: Author: Denes Solti
::
@echo off

echo Configuring git...

git config --global credential.helper store
echo "https://%GITHUB_REPO_TOKEN%:x-oauth-basic@github.com/sholtee/injector.git" >> %USERPROFILE%\.git-credentials
git config --global user.email %GITHUB_EMAIL%
git config --global user.name "[Bot]"