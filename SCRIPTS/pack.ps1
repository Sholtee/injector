#
# pack.ps1
#
# Author: Denes Solti
#
$currentbranch=Exec "git.exe" -commandArgs "rev-parse --abbrev-ref HEAD" -redirectOutput
Exec "dotnet.exe" -commandArgs "pack `"$($PROJECT.app | Resolve-Path)`" -c Release /p:CurrentBranch=$($currentbranch)"