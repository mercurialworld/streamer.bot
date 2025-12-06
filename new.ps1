param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$ProjectPath,
    
    [Parameter(Mandatory = $false)]
    [switch]$NoCode
)

$templatePath = "template.cs"
$projectFolder = Join-Path -Path "." -ChildPath "Projects\$ProjectPath"
$newFilePath = Join-Path -Path $projectFolder -ChildPath "Main.cs"

if ((Test-Path $projectFolder)) {
    Write-Error "Error: $newFilePath already exists. Aborting to prevent overwrite."
    exit 0
}

New-Item -ItemType Directory -Path $projectFolder -Force | Out-Null

Copy-Item $templatePath $newFilePath -Force

$namespaceSuffix = $ProjectPath -replace '[\\/]', '.'
$namespace = "SBot.Projects.$namespaceSuffix"

(Get-Content $newFilePath) `
    -replace "namespace SBot", "namespace $namespace" `
    -replace "public class Template", "public class Main" `
    | Set-Content $newFilePath

Write-Host "Created $newFilePath with namespace $namespace and class Main"

if (-not $NoCode) {
    code-insiders $newFilePath
}