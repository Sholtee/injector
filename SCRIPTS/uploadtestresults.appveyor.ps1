#
# uploadtestresults.appveyor.ps1
#
# Author: Denes Solti
#
$testresults=Path-Combine $PROJECT.artifacts, "testresults.xml"

if (Test-Path $testresults) {
  Write-Host Uploading test results...
  
  $client= New-Object System.Net.WebClient
  $client.UploadFile("https://ci.appveyor.com/api/testresults/nunit/$($Env:APPVEYOR_JOB_ID)", (Resolve-Path $testresults))
}

$coveragereport=Path-Combine $PROJECT.artifacts, "coverage.xml"

if (Test-Path $coveragereport) {
  Write-Host Uploading coverage report...

  Check (Exec "nuget.exe" -commandArgs "install coveralls.io -OutputDirectory `"$(Resolve-Path $PROJECT.vendor)`" -Version 1.4.2")

  Check (Exec (Path-Combine $PROJECT.vendor, "coveralls.io.1.4.2", "tools", "coveralls.net.exe" | Resolve-Path) -commandArgs "--opencover `"$(Resolve-Path $coveragereport)`" -r $Env:COVERALLS_REPO_TOKEN")
}