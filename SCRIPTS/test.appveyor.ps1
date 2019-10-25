#
# test.appveyor.ps1
#
# Author: Denes Solti
#
function Test() {
  if ($Env:APPVEYOR_REPO_TAG_NAME -eq "perf") {
    Write-Host Running performance tests...
    Performance-Tests
  } else {
    Write-Host Running regular tests...
    Regular-Tests
  }
}