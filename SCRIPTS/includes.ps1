#
# includes.ps1
#
# Author: Denes Solti
#
Get-ChildItem -path "." | where { $_.Name -match "\.ps1$" -and ($_.Name -ne (Split-Path $MyInvocation.PSCommandPath -Leaf)) } | foreach {
  .(".\$($_.Name)")
}
