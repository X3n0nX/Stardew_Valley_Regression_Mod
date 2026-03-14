param (
    [string]$TargetDir,
    [string]$ProjectDir,
	[string]$GameModsDir,
	[string]$SevenZipDir,
    [string]$ModVersion,
    [string]$DialogueVersion
)

Write-Host "TargetDir: $TargetDir"
Write-Host "ProjectDir: $ProjectDir"
Write-Host "GameModsDir: $GameModsDir"
Write-Host "SevenZipDir: $SevenZipDir"

$ErrorActionPreference = "Stop"

$StagingDir = Join-Path $TargetDir "\Staging"
$CoreStaging = "$StagingDir\Regression\Core"
$DialogueStaging = "$StagingDir\Regression\Dialogue"
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
if (-not (Test-Path $SevenZipDir)) {
    throw "7-Zip not found. Please install 7-Zip."
}
& $SevenZipDir a -tzip "$ZipPath" "$StagingSource" -mx9 -y | Out-Null

# 6. Copy ZIP to Releases folder up top
$ReleasesDir = Join-Path $ProjectDir "..\Releases"
if (-not (Test-Path $ReleasesDir)) { New-Item -ItemType Directory -Force $ReleasesDir | Out-Null }
Write-Host "Copying ZIP to $ReleasesDir"
Copy-Item -Path $ZipPath -Destination $ReleasesDir -Force

# 7. Deploy unzipped folder to local game folder (Skip if folder doesn't exist)
$DeployDir = Join-Path $GameModsDir "Regression"

if (Test-Path $GameModsDir) {
    Write-Host "Local game folder found. Deploying..."
    try {
        if (Test-Path $DeployDir) { Remove-Item -Recurse -Force $DeployDir -ErrorAction Stop }
        Copy-Item -Path (Join-Path $StagingDir "Regression") -Destination $GameModsDir -Recurse -Force -ErrorAction Stop
        Write-Host "Deployment successful!"
    }
    catch {
        Write-Warning "Could not deploy to local game folder. Skipping."
    }
}
else {
    Write-Host "Local game folder not found (Expected on Build Server). Skipping deployment."
}

Write-Host "Packaging complete!"