#
# pack.ps1
#
# Author: Denes Solti
#
Remove-Directory $PROJECT.bin

$currentbranch=Exec "git.exe" -commandArgs "rev-parse --abbrev-ref HEAD" -redirectOutput
Exec "dotnet.exe" -commandArgs "pack `"$($PROJECT.app | Resolve-Path)`" -c Release /p:CurrentBranch=$($currentbranch)"