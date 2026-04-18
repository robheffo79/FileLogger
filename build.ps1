#!/usr/bin/env pwsh
<#
.SYNOPSIS
Build script for HeffernanTech.Extensions.FileLogging NuGet package

.DESCRIPTION
Cleans the build, runs tests, and creates a NuGet package.
Exits with code 0 on success, non-zero on failure.

.EXAMPLE
.\build.ps1 -Release
#>

param(
    [switch]$Release = $false,
    [switch]$NoTest = $false
)

$ErrorActionPreference = "Stop"
$configuration = if ($Release) { "Release" } else { "Debug" }
$testProject = "HeffernanTech.Extensions.FileLogger.Tests/HeffernanTech.Extensions.FileLogger.Tests.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "HeffernanTech.Extensions.FileLogging" -ForegroundColor Cyan
Write-Host "Build & Package Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean
Write-Host "[1/4] Cleaning..." -ForegroundColor Yellow
try {
    dotnet clean --configuration $configuration --verbosity quiet 2>&1 | Out-Null
    Write-Host "✓ Clean successful" -ForegroundColor Green
}
catch {
    Write-Host "✗ Clean failed: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 2: Build
Write-Host "[2/4] Building ($configuration)..." -ForegroundColor Yellow
try {
    dotnet build --configuration $configuration --no-incremental
    Write-Host "✓ Build successful" -ForegroundColor Green
}
catch {
    Write-Host "✗ Build failed: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 3: Tests
if (-not $NoTest) {
    Write-Host "[3/4] Running tests..." -ForegroundColor Yellow
    try {
        $testOutput = dotnet test $testProject --configuration $configuration --no-build --verbosity quiet 2>&1
        if (($testOutput -match "Passed!") -and ($testOutput -match "Failed:.*0")) {
            Write-Host "✓ All tests passed" -ForegroundColor Green
        }
        else {
            Write-Host "✗ Tests failed" -ForegroundColor Red
            $testOutput | Select-Object -Last 10
            exit 1
        }
    }
    catch {
        Write-Host "✗ Test execution failed: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "[3/4] Skipping tests (NoTest switch)" -ForegroundColor Yellow
}
Write-Host ""

# Step 4: Pack
Write-Host "[4/4] Creating NuGet package..." -ForegroundColor Yellow
try {
    $packOutput = "..\nupkg"
    if (-not (Test-Path $packOutput)) {
        New-Item -ItemType Directory -Path $packOutput | Out-Null
    }

    dotnet pack `
        --configuration $configuration `
        --no-build `
        --output $packOutput `
        --verbosity minimal

    Write-Host "✓ Package created successfully" -ForegroundColor Green

    $nupkg = Get-ChildItem "$packOutput\HeffernanTech.Extensions.FileLogging.*.nupkg" -ErrorAction SilentlyContinue |
             Sort-Object LastWriteTime -Descending |
             Select-Object -First 1

    if ($nupkg) {
        $size = "{0:N2} KB" -f ($nupkg.Length / 1KB)
        $version = $nupkg.BaseName -replace 'HeffernanTech\.Extensions\.FileLogging\.', ''
        Write-Host ""
        Write-Host "Package: $($nupkg.Name)" -ForegroundColor Cyan
        Write-Host "Version: $version" -ForegroundColor Cyan
        Write-Host "Size: $size" -ForegroundColor Cyan
        Write-Host "Path: $($nupkg.FullName)" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "✗ Pack failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✓ Build complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
exit 0
