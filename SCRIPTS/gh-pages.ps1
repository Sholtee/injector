#
# gh-pages.ps1
#
# Author: Denes Solti
#
Write-Host Prepare DOCS repo...

$repodir=Path-Combine (Directory-Path $PROJECT.solution | Resolve-Path), $PROJECT.docsbranch
Remove-Directory $repodir

try {
  Exec "git.exe" -commandArgs "clone https://github.com/$($PROJECT.githubrepo) --branch `"$($PROJECT.docsbranch)`" `"$repodir`""
  $repodir=Resolve-Path $repodir

  $updateAPI=!(Path-Combine $PROJECT.artifacts, "BenchmarkDotNet.Artifacts" | Test-Path)

  if ($updateAPI) {
    Write-Host Building API docs...
  
    DocFx "$(Path-Combine (Directory-Path $PROJECT.app | Resolve-Path), 'docfx.json')"
  
    Write-Host Moving API docs...
  
    Move-Directory (Path-Combine $PROJECT.artifacts, "doc" | Resolve-Path) $repodir -clearDst
  } else {
    Write-Host Building benchmark docs...
	
	DocFx "$(Path-Combine (Directory-Path $PROJECT.perftests | Resolve-Path), 'docfx.json')"
	
	Write-Host Moving benchmark docs...
	
	Move-Directory (Path-Combine $PROJECT.artifacts, "BenchmarkDotNet.Artifacts", "perf" | Resolve-Path) $repodir -clearDst
  }

  Write-Host Committing changes...

  $oldLocation=Get-Location
  Set-Location -path $repodir

  try {
    if ($updateAPI) {
      Exec "git.exe" -commandArgs "add doc -A"
      Exec "git.exe" -commandArgs "commit -m `"docs up`""
	} else {
      Exec "git.exe" -commandArgs "add perf -A"
      Exec "git.exe" -commandArgs "commit -m `"benchmarks up`""	
	}
	
    Exec "git.exe" -commandArgs "push origin $($PROJECT.docsbranch)"
  } finally {
    Set-Location -path $oldLocation    
  }
} finally {
  Remove-Directory $repodir
}