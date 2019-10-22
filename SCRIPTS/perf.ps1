#
# perf.ps1
#
# Author: Denes Solti
#
$artifacts=Path-Combine $PROJECT.artifacts, "BenchmarkDotNet.Artifacts"

Remove-Directory $artifacts
Create-Directory $artifacts

Remove-Directory $PROJECT.bin
Create-Directory $PROJECT.bin

Check (Exec "dotnet.exe" -commandArgs "build $(Resolve-Path $PROJECT.perftests) --framework $($PROJECT.perftarget) --configuration Perf --output `"$(Resolve-Path $PROJECT.bin)`"")

$perfexe="$([io.path]::GetFileNameWithoutExtension($($PROJECT.perftests))).exe"

Check (Exec "$(Path-Combine $PROJECT.bin, $perfexe | Resolve-Path)" -commandArgs "-f * -e GitHub -a `"$(Resolve-Path $artifacts)`"")