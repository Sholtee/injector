#
# common.ps1
#
# Author: Denes Solti
#
$ErrorActionPreference = "Stop"

Set-Variable PROJECT -option Constant -value (Get-Content .\project.json -raw | ConvertFrom-Json)

function Create-Directory([Parameter(Position = 0)][string[]] $path) {
  if (!(Test-Path $path)) {
    New-Item -path $path -force -itemType "Directory" | Out-Null
  }
}

function Remove-Directory([Parameter(Position = 0)][string[]] $path) {
  if (Test-Path $path) {
    Remove-Item -path $path -force -recurse | Out-Null
  }
}

function Directory-Of([Parameter(Position = 0)][string] $filename) {
  $path = Join-Path "$(Get-Location)" $filename
  
  try {
    if (Test-Path $path) {
      return (Get-Item $path).Directory.FullName
    }
	
	return Directory-Of "$(Join-Path .. $filename)"
  } catch {
  }    
}

function Get-Solution-Directory() {
  return Directory-Of($PROJECT.solution)
}

function Exec([Parameter(Position = 0)][string]$command, [string]$commandArgs = $null) {
  $startInfo = New-Object System.Diagnostics.ProcessStartInfo
  $startInfo.FileName = $command
  $startInfo.Arguments = $commandArgs
  $startInfo.UseShellExecute = $false
  $startInfo.WorkingDirectory = Get-Location

  $process = New-Object System.Diagnostics.Process
  $process.StartInfo = $startInfo
  $process.Start() | Out-Null

  $finished = $false
  try {
    while (!$process.WaitForExit(100)) {
      # Non-blocking loop done to allow ctr-c interrupts
    }

    $finished = $true
    return $process.ExitCode
  }
  finally {
    if (!$finished) {
      $process.Kill()
    }
  }
}

