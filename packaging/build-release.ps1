param(
    [Parameter(Mandatory = $true)]
    [string]$Ymm4Directory,
    [Parameter(Mandatory = $true)]
    [string]$UtauTtsReleaseDirectory
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Version = "0.1.5"
$BuildRoot = Join-Path $ProjectRoot "build"
$RuntimeRoot = Join-Path $BuildRoot "utautts-runtime-v1.5.1"
$PackageRoot = Join-Path $BuildRoot "UTAUTalkVoice-$Version"
$DistRoot = Join-Path $ProjectRoot "dist"

foreach ($path in @($BuildRoot, $DistRoot)) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

& dotnet build (Join-Path $ProjectRoot "src\UTAUTalkVoice\UTAUTalkVoice.csproj") `
    -c Release -p:Ymm4Dir=$Ymm4Directory
if ($LASTEXITCODE -ne 0) { throw "Plugin build failed" }

if (Test-Path $RuntimeRoot) { Remove-Item -LiteralPath $RuntimeRoot -Recurse -Force }
New-Item -ItemType Directory -Path `
    "$RuntimeRoot\tools", `
    "$RuntimeRoot\models", `
    "$RuntimeRoot\renderer\utautts-world-phrase", `
    "$RuntimeRoot\runtime", `
    "$RuntimeRoot\licenses", `
    "$RuntimeRoot\docs" -Force | Out-Null

Copy-Item "$UtauTtsReleaseDirectory\tools\utautts-cli.exe" "$RuntimeRoot\tools\"
Copy-Item "$UtauTtsReleaseDirectory\models\frame-intonation-tcn-v12.json" "$RuntimeRoot\models\"
Copy-Item "$UtauTtsReleaseDirectory\models\README.md" "$RuntimeRoot\models\"
Copy-Item "$UtauTtsReleaseDirectory\renderer\README.md" "$RuntimeRoot\renderer\"
Copy-Item "$UtauTtsReleaseDirectory\renderer\utautts-world-phrase\renderer.json" "$RuntimeRoot\renderer\utautts-world-phrase\"
Copy-Item "$UtauTtsReleaseDirectory\runtime\utautts-worldline-bridge.exe" "$RuntimeRoot\runtime\"
Copy-Item "$UtauTtsReleaseDirectory\runtime\utautts-world-engine.dll" "$RuntimeRoot\runtime\"
Copy-Item "$UtauTtsReleaseDirectory\runtime\utautts-openjtalk-features.exe" "$RuntimeRoot\runtime\"
Copy-Item "$UtauTtsReleaseDirectory\runtime\open_jtalk_dic_utf_8-1.11" "$RuntimeRoot\runtime\" -Recurse
Copy-Item "$UtauTtsReleaseDirectory\runtime\licenses" "$RuntimeRoot\runtime\" -Recurse
Copy-Item "$UtauTtsReleaseDirectory\licenses\*" "$RuntimeRoot\licenses\" -Recurse
Copy-Item "$UtauTtsReleaseDirectory\LICENSE", `
    "$UtauTtsReleaseDirectory\LICENSE-SCOPE.md", `
    "$UtauTtsReleaseDirectory\THIRD_PARTY_NOTICES.txt", `
    "$UtauTtsReleaseDirectory\README.md" $RuntimeRoot
Copy-Item "$UtauTtsReleaseDirectory\docs\cli.md", `
    "$UtauTtsReleaseDirectory\docs\installation.md" "$RuntimeRoot\docs\"

$UtauTtsArchive = Join-Path $BuildRoot "utautts.zip"
Compress-Archive -Path "$RuntimeRoot\*" -DestinationPath $UtauTtsArchive -CompressionLevel Optimal -Force

if (Test-Path $PackageRoot) { Remove-Item -LiteralPath $PackageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $PackageRoot | Out-Null
Copy-Item (Join-Path $ProjectRoot "src\UTAUTalkVoice\bin\Release\net10.0-windows10.0.19041.0\UTAUTalkVoice.dll") $PackageRoot
Copy-Item (Join-Path $ProjectRoot "README.md") $PackageRoot
Copy-Item (Join-Path $ProjectRoot "LICENSE") $PackageRoot
Copy-Item (Join-Path $ProjectRoot "THIRD_PARTY_NOTICES.md") $PackageRoot
Copy-Item $UtauTtsArchive $PackageRoot

$ZipPath = Join-Path $DistRoot "UTAUTalkVoice-$Version.zip"
$YmmePath = Join-Path $DistRoot "UTAUTalkVoice-$Version.ymme"
Compress-Archive -Path "$PackageRoot\*" -DestinationPath $ZipPath -CompressionLevel Optimal -Force
Move-Item -LiteralPath $ZipPath -Destination $YmmePath -Force
Write-Host "Built: $YmmePath"
