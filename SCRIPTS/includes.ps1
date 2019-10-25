#
# includes.ps1
#
# Author: Denes Solti
#
Get-ChildItem -path "." | where { $_.Name -match "\.ps1$" -and ($_.Name -ne "includes.ps1") } | foreach {
  Write-Host "Including $($_.Name)"
  .("$($_.FullName)")
}
