$dir = Read-Host "Enter the root path of your StreamerBot"

$propsFile = "Directory.Build.props"

$content = @"
<Project>
  <PropertyGroup>
    <SBotRootDir>$dir</SBotRootDir>
  </PropertyGroup>
</Project>
"@

Set-Content -Path $propsFile -Value $content

Write-Host "Set SBotRootDir to $dir in $propsFile"
