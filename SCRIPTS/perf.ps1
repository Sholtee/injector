#
# perf.ps1
#
# Author: Denes Solti
#
.("$(Join-Path . common.ps1)")

$artifacts=Join-Path $PROJECT.artifacts BenchmarkDotNet.Artifacts

Remove-Directory $artifacts
Create-Directory $artifacts

Remove-Directory $PROJECT.bin
Create-Directory $PROJECT.bin

$result=Exec "dotnet.exe" -commandArgs "build $(Resolve-Path $PROJECT.perftests) --framework $($PROJECT.perftarget) --configuration Perf --output `"$(Resolve-Path $PROJECT.bin)`""

if ($result -Ne 0) {
  Exit $result
}

$perfexe="$([io.path]::GetFileNameWithoutExtension($($PROJECT.perftests))).exe"

Exit Exec "$(Join-Path $PROJECT.bin $perfexe | Resolve-Path)" -commandArgs "-f * -e GitHub -a `"$(Resolve-Path $artifacts)`""