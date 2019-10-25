#
# includes.ps1
#
# Author: Denes Solti
#
Get-ChildItem -path "." | where { $_.Name -match "\.ps1$" -and ($_.FullName -ne $MyInvocation.ScriptName) } | foreach {
  Write-Host "Including $($_.Name)"
  .("$($_.FullName)")
}
