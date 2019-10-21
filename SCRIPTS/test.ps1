#
# test.ps1
#
# Author: Denes Solti
#
.(".\common.ps1")
Exec "nuget.exe" -commandArgs "install OpenCover -OutputDirectory $($PROJECT.vendor) -Version 4.7.922" | Out-Null

if (!(Test-Path $PROJECT.artifacts)) {
  Create-Directory $PROJECT.artifacts
}

$opencover=Join-Path $PROJECT.vendor "OpenCover.4.7.922\tools\OpenCover.Console.exe" | Resolve-Path
$args="-target:`"$(Join-Path $Env:ProgramFiles dotnet\dotnet.exe)`" -targetargs:`"test '$(Resolve-Path $PROJECT.tests)' --framework:netcoreapp2.2 --configuration:Debug --test-adapter-path:. --logger:nunit;LogFilePath='$(Join-Path (Resolve-Path $PROJECT.artifacts) testresults.xml)'`" -output:`"$(Join-Path (Resolve-Path $PROJECT.artifacts) coverage.xml)`" -oldStyle -register:user -excludebyattribute:*.ExcludeFromCoverage* -filter:`"$($PROJECT.coveragefilter)`""

Write-Host $args

#Exec $opencover -commandArgs $args | Out-Null