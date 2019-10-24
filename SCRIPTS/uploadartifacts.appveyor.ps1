#
# uploadartifacts.appveyor.ps1
#
# Author: Denes Solti
#
function Upload-Artifacts() {
  Push-AppveyorArtifact (Path-Combine $PROJECT.artifacts, "log.txt")
  
  $errors=Path-Combine $PROJECT.artifacts, "errors.txt"
  
  if(Test-Path -path $errors) {
    Push-AppveyorArtifact $errors
  }  
}