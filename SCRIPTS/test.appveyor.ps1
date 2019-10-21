#
# test.appveyor.ps1
#
# Author: Denes Solti
#
if ($Env:APPVEYOR_REPO_TAG_NAME -eq "perf") {
  Write-Host Running performance tests...
  .("$(Join-Path . perf.ps1)")
} else {
  Write-Host Running regular tests...
  .("$(Join-Path . test.ps1)")
}