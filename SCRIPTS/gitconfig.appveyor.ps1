#
# gitconfig.appveyor.ps1
#
# Author: Denes Solti
#
Write-Host Configuring git...

Check (Exec "git.exe" -commandArgs "config --global credential.helper store")
Add-Content (Path-Combine $Env:USERPROFILE ".git-credentials") "https://$($Env:GITHUB_REPO_TOKEN):x-oauth-basic@github.com/$($PROJECT.githubrepo)"
Check (Exec "git.exe" -commandArgs "config --global user.email $($Env:GITHUB_EMAIL)")
Check (Exec "git.exe" -commandArgs "config --global user.name [Bot]")