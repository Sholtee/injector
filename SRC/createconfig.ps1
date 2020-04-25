#
# createconfig.ps1
#
# Author: Denes Solti
#
Param([Parameter(Mandatory = $true)][string] $path)

$ErrorActionPreference = "Stop"

function Get-CorPath(){
  [string]$base = (Get-Command dotnet).Path.Replace("dotnet.exe", "shared\Microsoft.NETCore.App")
  [string[]] $vers = (dir $base).Name.Split([System.Environment]::NewLine) | Where { $_ -Match "^\d+.\d+.\d+$" } | Sort-Object -Descending
  return Join-Path $base $vers[0]
}

$path = $path | Resolve-Path
[string] $corPath = Get-CorPath 

Add-Type -ReferencedAssemblies (Join-Path $corPath "netstandard.dll"), (Join-Path $corPath "System.Runtime.dll"), (Join-Path $corPath "System.Text.Json.dll"), $path -IgnoreWarnings -TypeDefinition @"
public class ConfigWriter
{
  public static void Write(string dstFile) 
  {
    var instance = new Solti.Utils.DI.Internals.Config();
    System.IO.File.WriteAllText(dstFile, System.Text.Json.JsonSerializer.Serialize(instance));
  }
}
"@

[ConfigWriter]::Write([System.IO.Path]::ChangeExtension($path, "config.json"))