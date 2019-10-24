#
# pack.ps1
#
# Author: Denes Solti
#
function Pack() {
  Remove-Directory $PROJECT.bin

  $currentbranch=Exec "git.exe" -commandArgs "rev-parse --abbrev-ref HEAD" -redirectOutput
  Exec "dotnet.exe" -commandArgs "pack `"$($PROJECT.app | Resolve-Path)`" -c Release /p:CurrentBranch=$($currentbranch)"
  
  return (Get-ChildItem -path (Path-Combine ($PROJECT.bin | Resolve-Path), "*.nupkg")).FullName
}