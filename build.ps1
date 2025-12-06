param(
    [Parameter(Mandatory = $true)]
    [string]$MainClassFile,
    [string]$OutputFile
)

# Configuration and constants
class Config {
    static [string[]] $MainFilePatterns = @(
        "Main.cs", "Program.cs", "CPHInline.cs", "*.Main.cs"
    )
    
    static [regex] $ProjectPathRegex = [regex]::new(
        '\\Projects\\(.+)\\[^\\]+$', 'Compiled'
    )
    
    static [regex] $ProjectRootRegex = [regex]::new(
        '^(.*?\\Projects)\\.+\\[^\\]+$', 'Compiled'
    )
    
    static [regex] $UsingRegex = [regex]::new(
        '^using\s+(?!Streamer\.bot|SBot\.)', 'Compiled'
    )
    
    static [regex] $SystemUsingRegex = [regex]::new(
        '^using\s+System(\..*)?;$', 'Compiled'
    )
    
    static [regex] $ClassNameRegex = [regex]::new(
        'public\s+class\s+(\w+)(\s*:\s*[\w\<\>\, ]+)?', 'Compiled'
    )
    
    static [regex] $NamespaceRegex = [regex]::new(
        '^\s*namespace\s+[\w\.]+(\s*\{|\s*;)', 'Compiled'
    )
    
    static [regex] $MainMethodRegex = [regex]::new(
        'class\s+\w+.*\{.*public\s+.*Execute\s*\(|static\s+void\s+Main\s*\(|public\s+bool\s+Execute\s*\(',
        'Compiled, Singleline'
    )
}

class PathResolver {
    static [hashtable] ExtractProjectInfo([string]$inputPath) {
        $resolvedInput = [PathResolver]::ResolveInput($inputPath)
        $resolvedPath = (Resolve-Path $resolvedInput).Path -replace '/', '\'
        
        $projectMatch = [Config]::ProjectPathRegex.Match($resolvedPath)
        if (-not $projectMatch.Success) {
            throw "Invalid project path format. Expected: '...\\Projects\\<ProjectPath>\\MainClassFile.cs'"
        }
        
        $rootMatch = [Config]::ProjectRootRegex.Match($resolvedPath)
        if (-not $rootMatch.Success) {
            throw "Could not determine project root from path."
        }
        
        return @{
            ProjectPath   = $projectMatch.Groups[1].Value
            ProjectRoot   = $rootMatch.Groups[1].Value
            SourceDir     = Split-Path $resolvedInput -Parent
            MainClass     = [System.IO.Path]::GetFileNameWithoutExtension($resolvedInput)
            MainClassFile = $resolvedInput
        }
    }
    
    static [string] ResolveInput([string]$inputPath) {
        # Direct .cs file path
        if ([System.IO.Path]::IsPathRooted($inputPath) -and 
            $inputPath.EndsWith('.cs') -and (Test-Path $inputPath)) {
            return $inputPath
        }
        
        # Relative .cs file path
        if ($inputPath.EndsWith('.cs') -and (Test-Path $inputPath)) {
            return (Resolve-Path $inputPath).Path
        }
        
        # Find project and main class file
        $projectDir = [PathResolver]::FindProjectDirectory($inputPath)
        return [PathResolver]::FindMainClassFile($projectDir)
    }
    
    static [string] FindProjectDirectory([string]$inputPath) {
        $projectsDir = [PathResolver]::FindProjectsDirectory()
        $normalizedInput = $inputPath -replace '/', '\' -replace '^Projects\\', ''
        
        # Try direct path first
        $directPath = Join-Path $projectsDir $normalizedInput
        if (Test-Path $directPath) {
            return $directPath
        }
        
        # Search recursively
        $projectName = Split-Path $normalizedInput -Leaf
        $matchingDirs = Get-ChildItem -Path $projectsDir -Directory -Recurse |
        Where-Object { $_.Name -eq $projectName -or $_.FullName -like "*$normalizedInput*" } |
        Select-Object -First 1
        
        if ($matchingDirs) {
            Write-Host "Found project: $($matchingDirs.FullName)"
            return $matchingDirs.FullName
        }
        
        throw "Project '$inputPath' not found in $projectsDir"
    }
    
    static [string] FindProjectsDirectory() {
        $searchDir = Get-Location
        while ($searchDir) {
            $testDir = Join-Path $searchDir "Projects"
            if (Test-Path $testDir) { return $testDir }
            
            $parent = Split-Path $searchDir -Parent
            if ($parent -eq $searchDir) { break }
            $searchDir = $parent
        }
        throw "Projects directory not found"
    }
    
    static [string] FindMainClassFile([string]$projectDir) {
        # Try common patterns first
        foreach ($pattern in [Config]::MainFilePatterns) {
            $files = Get-ChildItem -Path $projectDir -Filter $pattern -Recurse
            if ($files) {
                $rootFile = $files | Where-Object { 
                (Split-Path $_.FullName -Parent) -eq $projectDir 
                } | Select-Object -First 1 
            
                if ($rootFile) {
                    return $rootFile.FullName
                }
                else {
                    return $files[0].FullName
                }
            }
        }
    
        # Fallback: search for files with main methods
        $csFiles = Get-ChildItem -Path $projectDir -Filter "*.cs" -Recurse
        if (-not $csFiles) {
            throw "No .cs files found in: $projectDir"
        }
    
        foreach ($file in $csFiles) {
            $content = Get-Content $file.FullName -Raw
            if ([Config]::MainMethodRegex.IsMatch($content)) {
                return $file.FullName
            }
        }
    
        Write-Warning "Could not determine main class. Using: $($csFiles[0].Name)"
        return $csFiles[0].FullName
    }

    
    static [string] ResolveOutputPath([string]$outputFile, [hashtable]$projectInfo) {
        if (-not $outputFile) {
            $buildDir = Join-Path $projectInfo.ProjectRoot "..\Builds\$($projectInfo.ProjectPath)"
            [PathResolver]::EnsureDirectory($buildDir)
            return Join-Path $buildDir "CPHInline.sbot"
        }
        
        if ([System.IO.Path]::IsPathRooted($outputFile)) {
            [PathResolver]::EnsureDirectory((Split-Path $outputFile -Parent))
            return $outputFile
        }
        
        $buildDir = Join-Path $projectInfo.ProjectRoot "..\Builds\$($projectInfo.ProjectPath)"
        $parentDir = Split-Path $outputFile -Parent
        if ($parentDir) { $buildDir = Join-Path $buildDir $parentDir }
        
        [PathResolver]::EnsureDirectory($buildDir)
        return Join-Path $buildDir (Split-Path $outputFile -Leaf)
    }
    
    static [void] EnsureDirectory([string]$path) {
        if ($path -and -not (Test-Path $path)) {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
        }
    }
}

class UsingProcessor {
    static [string[]] ProcessUsings([System.IO.FileInfo[]]$csFiles) {
        $usingsSet = [System.Collections.Generic.HashSet[string]]::new()
        
        foreach ($file in $csFiles) {
            $content = Get-Content $file.FullName -Raw
            $lines = $content -split "`r?`n"
            
            foreach ($line in $lines) {
                $trimmedLine = $line.Trim()
                if ([Config]::UsingRegex.IsMatch($trimmedLine)) {
                    $usingsSet.Add($trimmedLine) | Out-Null
                }
            }
        }
        
        return [UsingProcessor]::OrganizeUsings($usingsSet)
    }
    
    static [string[]] OrganizeUsings([System.Collections.Generic.HashSet[string]]$usings) {
        $systemRoot = [System.Collections.Generic.List[string]]::new()
        $systemSub = [System.Collections.Generic.List[string]]::new()
        $others = [System.Collections.Generic.List[string]]::new()
        
        foreach ($using in $usings) {
            if ($using -eq 'using System;') {
                $systemRoot.Add($using)
            }
            elseif ($using.StartsWith('using System.')) {
                $systemSub.Add($using)
            }
            else {
                $others.Add($using)
            }
        }
        
        # Sort and combine
        $result = [System.Collections.Generic.List[string]]::new()
        if ($systemRoot.Count -gt 0) { $result.AddRange($systemRoot) }
        if ($systemSub.Count -gt 0) { 
            $systemSub.Sort()
            $result.AddRange($systemSub) 
        }
        if (($systemRoot.Count -gt 0 -or $systemSub.Count -gt 0) -and $others.Count -gt 0) {
            $result.Add("")
        }
        if ($others.Count -gt 0) { 
            $others.Sort()
            $result.AddRange($others) 
        }
        
        return $result.ToArray()
    }
}

class ContentProcessor {
    static [hashtable] ProcessFiles([System.IO.FileInfo[]]$csFiles, [string]$mainClassFile, [string]$mainClassName) {
        $resolvedMainClass = (Resolve-Path $mainClassFile).Path
        $mainClassContent = $null
        $otherContents = [System.Collections.Generic.List[string]]::new()
        
        foreach ($file in $csFiles) {
            $content = [ContentProcessor]::ProcessFileContent($file.FullName)
            if (-not $content) { continue }
            
            if ($file.FullName -eq $resolvedMainClass) {
                $mainClassContent = [ContentProcessor]::ProcessMainClass($content, $mainClassName)
            }
            else {
                $otherContents.Add($content)
            }
        }
        
        if (-not $mainClassContent) {
            throw "Main class content not found in: $mainClassFile"
        }
        
        return @{
            MainClass    = $mainClassContent
            OtherClasses = $otherContents.ToArray()
        }
    }
    
    static [string] ProcessMainClass([string]$content, [string]$expectedName) {
        $match = [Config]::ClassNameRegex.Match($content)
        if ($match.Success) {
            $actualClassName = $match.Groups[1].Value
            $content = [Config]::ClassNameRegex.Replace($content, 'public class CPHInline', 1)
        }
        return $content.Trim()
    }
    
    static [string] ProcessFileContent([string]$filePath) {
        $content = Get-Content $filePath -Raw
        if (-not $content) { return "" }
        
        # Remove using statements and process namespaces
        $lines = $content -split "`r?`n" | Where-Object { $_ -notmatch '^using ' }
        $content = ($lines -join "`n").Trim()
        
        return [ContentProcessor]::RemoveNamespaces($content)
    }
    
    static [string] RemoveNamespaces([string]$code) {
        if (-not $code) { return "" }
        
        $lines = $code -split "`n"
        $output = [System.Collections.Generic.List[string]]::new()
        $insideNamespace = $false
        $braceDepth = 0
        
        foreach ($line in $lines) {
            if ([Config]::NamespaceRegex.IsMatch($line)) {
                if ($line -match '\s*;\s*$') { continue }  # File-scoped namespace
                $insideNamespace = $true
                $braceDepth = if ($line -match '\{') { 1 } else { 0 }
                continue
            }
            
            if ($insideNamespace) {
                $braceDepth += ([regex]::Matches($line, '\{').Count - [regex]::Matches($line, '\}').Count)
                if ($braceDepth -le 0) {
                    $insideNamespace = $false
                    $braceDepth = 0
                    continue
                }
            }
            
            $output.Add($line)
        }
        
        return $output -join "`n"
    }
}

class OutputWriter {
    static [void] WriteOutput([string]$outputPath, [string[]]$usings, [hashtable]$content) {
        $builder = [System.Text.StringBuilder]::new()
        
        # Add usings
        foreach ($using in $usings) {
            $builder.AppendLine($using) | Out-Null
        }
        
        if ($usings.Count -gt 0) {
            $builder.AppendLine() | Out-Null
        }
        
        # Add main class
        $builder.AppendLine($content.MainClass) | Out-Null
        
        # Add other classes
        foreach ($class in $content.OtherClasses) {
            $builder.AppendLine($class) | Out-Null
        }
        
        # Write to file with proper encoding
        $finalContent = $builder.ToString() -replace "`r`n", "`n" -replace "`r", "`n"
        Set-Content -Path $outputPath -Value $finalContent -Encoding UTF8 -NoNewline
    }
}

function Main {
    try {
        Write-Host "Resolving input: $MainClassFile"
        
        $projectInfo = [PathResolver]::ExtractProjectInfo($MainClassFile)
        $outputPath = [PathResolver]::ResolveOutputPath($OutputFile, $projectInfo)
        
        Write-Host "Main class file: $($projectInfo.MainClassFile)"
        Write-Host "Project path: $($projectInfo.ProjectPath)"
        Write-Host "Output: $outputPath"
        
        $csFiles = Get-ChildItem -Path $projectInfo.SourceDir -Recurse -Filter *.cs | 
        Sort-Object FullName
        
        if (-not $csFiles) {
            throw "No .cs files found in $($projectInfo.SourceDir)"
        }
        
        Write-Host "Processing $($csFiles.Count) .cs files..."
        
        $usings = [UsingProcessor]::ProcessUsings($csFiles)
        $content = [ContentProcessor]::ProcessFiles($csFiles, $projectInfo.MainClassFile, $projectInfo.MainClass)
        
        [OutputWriter]::WriteOutput($outputPath, $usings, $content)
        
        Write-Host "Successfully built: $outputPath" -ForegroundColor Green
    }
    catch {
        Write-Error "Build failed: $($_.Exception.Message)"
        exit 1
    }
}

Main
