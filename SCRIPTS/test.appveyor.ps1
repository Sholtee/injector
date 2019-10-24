#
# test.appveyor.ps1
#
# Author: Denes Solti
#
function Test() {
  if ($Env:APPVEYOR_REPO_TAG_NAME -eq "perf") {
    Write-Host Running performance tests...
    Run-Script ".\perf.ps1"
  } else {
    Write-Host Running regular tests...
    Run-Script ".\test.ps1"
  }
}