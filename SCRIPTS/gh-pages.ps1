
Write-Host Prepare DOCS repo...

$repodir=Path-Combine (Directory-Path $PROJECT.solution | Resolve-Path), $PROJECT.docsbranch

Remove-Directory $repodir
Check (Exec "git.exe" -commandArgs "clone https://github.com/$($PROJECT.githubrepo) --branch `"$($PROJECT.docsbranch)`" `"$repodir`"")
$repodir=Resolve-Path $repodir

$benchmarkArtifacts=Path-Combine $PROJECT.artifacts, "BenchmarkDotNet.Artifacts"

if (!(Test-Path $benchmarkArtifacts)) {
  Write-Host Building API docs...
  
  Check (DocFx "$(Path-Combine (Directory-Path $PROJECT.app | Resolve-Path), 'docfx.json')")
  
  Write-Host Moving API docs...
  
  Move-Directory (Path-Combine $PROJECT.artifacts, "doc" | Resolve-Path) $repodir -clearDst
}