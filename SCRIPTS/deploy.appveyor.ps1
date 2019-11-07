#
# deploy.appveyor.ps1
#
# Author: Denes Solti
#
function Deploy() {
  if ($Env:APPVEYOR_REPO_TAG_NAME -match "^v\d+.\d+.\d+[-\w]*$") {
    Write-Host Deploying...
	
    $nupkgPath=Pack
    Exec "nuget.exe" -commandArgs "push `"$nupkgPath`" -s https://api.nuget.org/v3/index.json -k $($Env:NUGET_REPO_TOKEN)"
  }
}