#
# docfx.ps1
#
# Author: Denes Solti
#
function DocFx([Parameter(Position = 0)][string] $jsonpath) {
  Create-Directory $PROJECT.vendor
  
  Exec "nuget.exe" -commandArgs "install docfx.console -OutputDirectory `"$(Resolve-Path $PROJECT.vendor)`" -Version 2.46.0"
  
  Exec (Path-Combine $PROJECT.vendor, "docfx.console.2.46.0", "tools", "docfx.exe" | Resolve-Path) -commandArgs $jsonpath
}