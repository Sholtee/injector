#
# gitconfig.appveyor.ps1
#
# Author: Denes Solti
#
function Git-Config() {
  Write-Host Configuring git...

  Exec "git.exe" -commandArgs "config --global credential.helper store"
  Add-Content (Path-Combine $Env:USERPROFILE, ".git-credentials") "https://$($Env:GITHUB_REPO_TOKEN):x-oauth-basic@github.com/$($PROJECT.githubrepo)"
  Exec "git.exe" -commandArgs "config --global user.email $($Env:GITHUB_EMAIL)"
  Exec "git.exe" -commandArgs "config --global user.name [Bot]"
}