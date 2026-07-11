# SC Launch Build Script
# Generates: SC_Launch_windows.zip and SC_Launch_windows_install.exe

param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot
$ReleaseDir = Join-Path $ProjectDir "bin\Release\net8.0-windows"
$OutputDir = Join-Path $ProjectDir "dist"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   SC Launch Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Build project
if (-not $SkipBuild) {
    Write-Host "[1/5] Building project..." -ForegroundColor Yellow
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build completed!" -ForegroundColor Green
} else {
    Write-Host "[1/5] Skipping build (using existing Release)" -ForegroundColor Yellow
}

# Check Release directory
if (-not (Test-Path $ReleaseDir)) {
    Write-Host "Release directory not found: $ReleaseDir" -ForegroundColor Red
    exit 1
}

# 2. Create output directory
Write-Host "[2/5] Creating output directory..." -ForegroundColor Yellow
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# 3. Generate ZIP portable package
Write-Host "[3/5] Generating SC_Launch_windows.zip..." -ForegroundColor Yellow
$ZipPath = Join-Path $OutputDir "SC_Launch_windows.zip"
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

# Create temp directory for packaging
$TempZipDir = Join-Path $env:TEMP "SC_Launch_Zip_Temp"
if (Test-Path $TempZipDir) {
    Remove-Item $TempZipDir -Recurse -Force
}
New-Item -ItemType Directory -Path $TempZipDir -Force | Out-Null

# Copy Release files
Copy-Item -Path "$ReleaseDir\*" -Destination $TempZipDir -Recurse -Force

# Delete unnecessary files
$FilesToDelete = @("SC Launch.pdb", "*.deps.json")
foreach ($Pattern in $FilesToDelete) {
    Get-ChildItem -Path $TempZipDir -Filter $Pattern -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
}

# Create ZIP
Compress-Archive -Path "$TempZipDir\*" -DestinationPath $ZipPath -CompressionLevel Optimal
Remove-Item $TempZipDir -Recurse -Force

$ZipSize = [math]::Round((Get-Item $ZipPath).Length / 1MB, 2)
Write-Host "ZIP generated: $ZipPath ($ZipSize MB)" -ForegroundColor Green

# 4. Generate EXE installer
Write-Host "[4/5] Generating SC_Launch_windows_install.exe..." -ForegroundColor Yellow

# Check Inno Setup
$InnoSetupPath = $null
$PossiblePaths = @(
    "D:\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

foreach ($Path in $PossiblePaths) {
    if (Test-Path $Path) {
        $InnoSetupPath = $Path
        break
    }
}

if ($InnoSetupPath) {
    Write-Host "Found Inno Setup: $InnoSetupPath" -ForegroundColor Green
    
    # Run Inno Setup compiler
    $ISScript = Join-Path $ProjectDir "installer.iss"
    if (Test-Path $ISScript) {
        & $InnoSetupPath $ISScript
        if ($LASTEXITCODE -eq 0) {
            $ExePath = Join-Path $OutputDir "SC_Launch_windows_install.exe"
            if (Test-Path $ExePath) {
                $ExeSize = [math]::Round((Get-Item $ExePath).Length / 1MB, 2)
                Write-Host "EXE installer generated: $ExePath ($ExeSize MB)" -ForegroundColor Green
            }
        } else {
            Write-Host "Inno Setup compilation failed!" -ForegroundColor Red
        }
    } else {
        Write-Host "installer.iss script not found!" -ForegroundColor Red
    }
} else {
    Write-Host "Inno Setup not found! Please install Inno Setup 6." -ForegroundColor Yellow
    Write-Host "Download: https://jrsoftware.org/isinfo.php" -ForegroundColor Cyan
    Write-Host "Skipping EXE installer generation..." -ForegroundColor Yellow
}

# 5. Done
Write-Host "[5/5] Build completed!" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Output files:" -ForegroundColor Cyan
Write-Host "  - ZIP portable: $ZipPath" -ForegroundColor White
if (Test-Path (Join-Path $OutputDir "SC_Launch_windows_install.exe")) {
    Write-Host "  - EXE installer: $(Join-Path $OutputDir 'SC_Launch_windows_install.exe')" -ForegroundColor White
}
Write-Host "========================================" -ForegroundColor Cyan