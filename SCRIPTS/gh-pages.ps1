#
# gh-pages.ps1
#
# Author: Denes Solti
#
function GH-Pages() {
  Write-Host Prepare DOCS repo...

  $repodir=Path-Combine (Directory-Path $PROJECT.solution | Resolve-Path), $PROJECT.docsbranch
  Remove-Directory $repodir

  try {
    Exec "git.exe" -commandArgs "clone https://github.com/$($PROJECT.githubrepo) --branch `"$($PROJECT.docsbranch)`" `"$repodir`""
    $repodir=Resolve-Path $repodir

    function BuildNMove([string] $projectDir, [string] $docsOutput) {
      DocFx "$(Path-Combine $projectDir, 'docfx.json')"
      Write-Host "Moving docs..."
      Move-Directory $docsOutput $repodir -clearDst	
    }

    $updateAPI=!(Path-Combine $PROJECT.artifacts, "BenchmarkDotNet.Artifacts" | Test-Path)

    if ($updateAPI) {
      Write-Host Building API docs...
      BuildNMove -projectDir (Directory-Path $PROJECT.app) -docsOutput (Path-Combine $PROJECT.artifacts, "doc")
    } else {
      Write-Host Building benchmark docs...
      BuildNMove -projectDir (Directory-Path $PROJECT.perftests) -docsOutput (Path-Combine $PROJECT.artifacts, "BenchmarkDotNet.Artifacts", "perf")
    }

    function Commit([string] $message) {
      Write-Host Committing changes...
      $oldLocation=Get-Location
      Set-Location -path $repodir
      try {
        #Adding folders would freeze git.exe =(
        Get-ChildItem -path "." -Recurse | where { !$_.PSIsContainer } | foreach {
          Exec "git.exe" -commandArgs "add `"$($_.FullName)`"" -ignoreError
        }       
        Exec "git.exe" -commandArgs "commit -m `"$($message)`"" -ignoreError
        Exec "git.exe" -commandArgs "push origin $($PROJECT.docsbranch)"
      } finally {
        Set-Location -path $oldLocation	  
      }	  
    }

    if ($updateAPI) {
      Commit -message "docs up"
    } else {
      Commit -message "benchmarks up"	
    }	
  } finally {
    Remove-Directory $repodir
  }
}