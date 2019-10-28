#
# common.ps1
#
# Author: Denes Solti
#
$ErrorActionPreference = "Stop"

Set-Variable PROJECT -option Constant -value (Get-Content ".\project.json" -raw | ConvertFrom-Json)

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

function Path-Combine([Parameter(Position = 0)][string[]] $path) {
  return [System.IO.Path]::Combine($path)
}

function Path-Add-Slash([Parameter(Position = 0)][string] $path) {
  $sep=[System.IO.Path]::DirectorySeparatorChar
  
  if ($path -notmatch "\$($sep)$") {
    $path += $sep
  }
  
  return $path
}

function Move-Directory([Parameter(Position = 0)][string] $src, [Parameter(Position = 1)][string] $dst, [switch] $clearDst) {
  $src=Path-Add-Slash $src

  if (!(Test-Path $src)) {
    throw "`"$($src)`" could not be found"
  }
  
  $dst=Path-Add-Slash $dst
  
  if ($clearDst) {
    Remove-Directory (Path-Combine $dst, (Directory-Name $src))
  }
  
  Move-Item -path $src -destination $dst -force | Out-Null
}

function Directory-Path([Parameter(Position = 0)][string] $path) {
  return [System.IO.Path]::GetDirectoryName($path)
}

function Directory-Name([Parameter(Position = 0)][string] $path) {
  return  (New-Object System.IO.DirectoryInfo -ArgumentList (Directory-Path $path)).Name
}

function Directory-Of([Parameter(Position = 0)][string] $filename) {
  $path = Path-Combine Get-Location $filename
  
  try {
    if (Test-Path $path) {
      return Directory-Name $path
    }
	
    return Directory-Of "$(Path-Combine '..', $filename)"
  } catch {
  }    
}

function FileName-Without-Extension([Parameter(Position = 0)][string] $filename) {
  return [System.IO.Path]::GetFileNameWithoutExtension($filename)
}

function Write-Log([Parameter(ValueFromPipeline)][string] $text, [Parameter(Position = 0)][string] $filename) {
  Create-Directory $PROJECT.artifacts
  $text | Out-File (Path-Combine $PROJECT.artifacts, $filename) -force -append
}

function Exec([Parameter(Position = 0)][string]$command, [string]$commandArgs = $null, [switch] $redirectOutput, [switch] $noLog, [switch] $ignoreError) {
  $startInfo = New-Object System.Diagnostics.ProcessStartInfo
  $startInfo.FileName = $command
  $startInfo.Arguments = $commandArgs
  $startInfo.UseShellExecute = $false
  $startInfo.RedirectStandardOutput = ($redirectOutput -or !$noLog)
  $startInfo.RedirectStandardError = !$noLog 
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
    $exitCode = $process.ExitCode
	
    if ($exitCode -Ne 0) {
      if (!$noLog) {
        $process.StandardOutput.ReadToEnd() | Write-Log -filename "log.txt"
        $process.StandardError.ReadToEnd()  | Write-Log -filename "errors.txt"
      }
		
      if (!$ignoreError) {	
        Exit $exitCode
      }
	  
      return
    }
	
    if ($redirectOutput -or !$noLog) {
      $output=$process.StandardOutput.ReadToEnd()
	
      if (!$noLog) {
        $output | Write-Log -filename "log.txt"
      }
	
      if ($redirectOutput) {
        return $output
      }
    }
  } finally {
    if (!$finished) {
      $process.Kill()
    }
  }
}