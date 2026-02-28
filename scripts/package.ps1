param (
    [string]$TargetDir,
    [string]$ProjectDir,
    [string]$ModVersion,
    [string]$DialogueVersion
)

$ErrorActionPreference = "Stop"

$StagingDir = Join-Path $TargetDir "Staging"
$CoreStaging = Join-Path $StagingDir "Regression\Core"
$DialogueStaging = Join-Path $StagingDir "Regression\Dialogue"
$ZipName = "Regression Mod v$ModVersion.zip"
$ZipPath = Join-Path $TargetDir $ZipName

# 1. Clean up
Write-Host "Cleaning up staging directories..."
if (Test-Path $StagingDir) { Remove-Item -Recurse -Force $StagingDir }
if (Test-Path $ZipPath) { Remove-Item -Force $ZipPath }

# 2. Create structure
New-Item -ItemType Directory -Force $CoreStaging | Out-Null
New-Item -ItemType Directory -Force $DialogueStaging | Out-Null

# 3. Copy files to Staging
Write-Host "Copying files to staging..."
Copy-Item -Path "$TargetDir\*" -Destination $CoreStaging -Recurse -Force -Exclude "Staging", "Regression Dialogue", "Regression Mod", "*.zip", "*.pdb"
Copy-Item -Path "$TargetDir\Regression Dialogue\*" -Destination $DialogueStaging -Recurse -Force

# 4. Sync manifest versions
Write-Host "Syncing manifest versions..."
$CoreManifest = Join-Path $CoreStaging "manifest.json"
$DialogueManifest = Join-Path $DialogueStaging "manifest.json"

if (Test-Path $CoreManifest) {
    (Get-Content $CoreManifest) -replace '("Version":\s*)"[^"]*"', ('$1' + "`"$ModVersion`"") | Set-Content $CoreManifest
}

if (Test-Path $DialogueManifest) {
    (Get-Content $DialogueManifest) -replace '("Version":\s*)"[^"]*"', ('$1' + "`"$DialogueVersion`"") | Set-Content $DialogueManifest
}

# 5. Create Cross-Platform ZIP using 7z
# Using 7z ensures forward slashes and standard compression that works everywhere
Write-Host "Creating cross-platform ZIP with 7z: $ZipPath"
$StagingSource = Join-Path $StagingDir "Regression\*"
# -tzip: create zip file
# -mx9: ultra compression
# -y: assume yes on all queries
& 7z a -tzip "$ZipPath" "$StagingSource" -mx9 -y | Out-Null

# 6. Copy ZIP to Releases folder up top
$ReleasesDir = Join-Path $ProjectDir "..\Releases"
if (-not (Test-Path $ReleasesDir)) { New-Item -ItemType Directory -Force $ReleasesDir | Out-Null }
Write-Host "Copying ZIP to $ReleasesDir"
Copy-Item -Path $ZipPath -Destination $ReleasesDir -Force

# 7. Deploy unzipped folder to local game folder
$GameModsDir = "D:\SteamLibrary\steamapps\common\Stardew Valley\Mods"
$DeployDir = Join-Path $GameModsDir "Regression"

if (Test-Path $GameModsDir) {
    Write-Host "Deploying folder to game mods directory..."
    try {
        if (Test-Path $DeployDir) { Remove-Item -Recurse -Force $DeployDir -ErrorAction Stop }
        # Copy the staged folder, not the zip
        Copy-Item -Path (Join-Path $StagingDir "Regression") -Destination $GameModsDir -Recurse -Force -ErrorAction Stop
        Write-Host "Deployment successful!"
    }
    catch {
        Write-Warning "Could not deploy to game folder (files might be in use). Skipping deployment."
        Write-Warning "Error: $($_.Exception.Message)"
    }
}
else {
    Write-Host "Game folder not found, skipping local deployment."
}

Write-Host "Packaging complete!"
