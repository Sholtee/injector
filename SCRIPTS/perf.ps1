#
# perf.ps1
#
# Author: Denes Solti
#
.(".\common.ps1")

$artifacts=Path-Combine $PROJECT.artifacts, "BenchmarkDotNet.Artifacts"

Remove-Directory $artifacts
Create-Directory $artifacts

Remove-Directory $PROJECT.bin
Create-Directory $PROJECT.bin

$result=Exec "dotnet.exe" -commandArgs "build $(Resolve-Path $PROJECT.perftests) --framework $($PROJECT.perftarget) --configuration Perf --output `"$(Resolve-Path $PROJECT.bin)`""

if ($result -Ne 0) {
  Exit $result
}

$perfexe="$([io.path]::GetFileNameWithoutExtension($($PROJECT.perftests))).exe"

Exit Exec "$(Path-Combine $PROJECT.bin, $perfexe | Resolve-Path)" -commandArgs "-f * -e GitHub -a `"$(Resolve-Path $artifacts)`""