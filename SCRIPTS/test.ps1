#
# test.ps1
#
# Author: Denes Solti
#
.("$(Join-Path . common.ps1)")

Create-Directory $PROJECT.vendor

$result = Exec "nuget.exe" -commandArgs "install OpenCover -OutputDirectory `"$(Resolve-Path $PROJECT.vendor)`" -Version 4.7.922"

if ($result -Ne 0) {
  Exit $result
}

Remove-Directory $PROJECT.artifacts
Create-Directory $PROJECT.artifacts

$opencover=Join-Path $PROJECT.vendor "OpenCover.4.7.922\tools\OpenCover.Console.exe" | Resolve-Path

$args="
  -target:`"$(Join-Path $Env:ProgramFiles dotnet\dotnet.exe)`"
  -targetargs:`"test $(Resolve-Path $PROJECT.tests) --framework $($PROJECT.testtarget) --configuration:Debug --test-adapter-path:. --logger:nunit;LogFilePath=$(Join-Path (Resolve-Path $PROJECT.artifacts) testresults.xml)`"
  -output:`"$(Join-Path (Resolve-Path $PROJECT.artifacts) coverage.xml)`"
  -oldStyle 
  -register:user 
  -excludebyattribute:*.ExcludeFromCoverage* 
  -filter:`"$($PROJECT.coveragefilter)`"
"

Exit Exec $opencover -commandArgs $args