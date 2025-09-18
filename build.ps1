#!/usr/bin/env pwsh
# GUIDevMode Build and Deploy Script
# Run this script to build the mod and optionally copy to RimWorld

param(
    [switch]$Deploy,
    [string]$RimWorldPath = $env:RIMWORLD_PATH
)

Write-Host "Building GUIDevMode..." -ForegroundColor Green

# Build the project
try {
    Push-Location "Source\GUIDevMode"
    dotnet restore
    dotnet build -c Release
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build successful!" -ForegroundColor Green
        $dllPath = "..\..\Source\Assemblies\GUIDevMode.dll"
        $dllInfo = Get-Item $dllPath
        $sizeKB = [math]::Round($dllInfo.Length/1024, 1)
        Write-Host "DLL created: $($dllInfo.Name) ($sizeKB KB)" -ForegroundColor Cyan
    } else {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}

# Deploy to RimWorld if requested
if ($Deploy) {
    if (-not $RimWorldPath) {
        # Try to auto-detect RimWorld path
        $steamPaths = @(
            "C:\Program Files (x86)\Steam\steamapps\common\RimWorld",
            "C:\Program Files\Steam\steamapps\common\RimWorld"
        )
        
        foreach ($path in $steamPaths) {
            if (Test-Path "$path\RimWorldWin64_Data\Managed\Assembly-CSharp.dll") {
                $RimWorldPath = $path
                break
            }
        }
    }
    
    if ($RimWorldPath -and (Test-Path $RimWorldPath)) {
        Write-Host "Deploying to RimWorld..." -ForegroundColor Blue
        
        $modDestination = Join-Path $RimWorldPath "Mods\GUIDevMode"
        $assemblyDest = Join-Path $modDestination "Assemblies"
        
        # Create directories
        New-Item -ItemType Directory -Force -Path $assemblyDest | Out-Null
        
        # Copy DLL
        Copy-Item -Path "Source\Assemblies\GUIDevMode.dll" -Destination $assemblyDest -Force
        
        # Copy mod files (About, Defs)
        Copy-Item -Path "About" -Destination $modDestination -Recurse -Force
        Copy-Item -Path "Defs" -Destination $modDestination -Recurse -Force
        
        Write-Host "Deployed to: $modDestination" -ForegroundColor Green
        Write-Host "You can now enable the mod in RimWorld!" -ForegroundColor Yellow
    } else {
        Write-Host "RimWorld path not found. Set env:RIMWORLD_PATH or use -RimWorldPath parameter" -ForegroundColor Yellow
        Write-Host "Example: .\build.ps1 -Deploy -RimWorldPath 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld'" -ForegroundColor Gray
    }
}

Write-Host "Build complete!" -ForegroundColor Green