<#
.SYNOPSIS
    Signs executables using a code signing certificate

.DESCRIPTION
    This script signs executable files using signtool.exe from the Windows SDK.
    It supports both PFX file-based signing and certificate store-based signing.

.PARAMETER PfxPath
    Path to the PFX certificate file

.PARAMETER Password
    Password for the PFX file (as SecureString)

.PARAMETER Thumbprint
    Certificate thumbprint (alternative to PfxPath for store-based signing)

.PARAMETER FilesToSign
    Array of file paths to sign

.PARAMETER TimestampServer
    URL of the timestamp server (default: DigiCert)

.EXAMPLE
    .\sign-code.ps1 -PfxPath ".\cert.pfx" -Password $securePassword -FilesToSign @(".\app.exe", ".\app.dll")

.EXAMPLE
    .\sign-code.ps1 -Thumbprint "ABC123..." -FilesToSign @(".\app.exe")
#>

param(
    [string]$PfxPath,
    [SecureString]$Password,
    [string]$Thumbprint,
    [Parameter(Mandatory=$true)]
    [string[]]$FilesToSign,
    [string]$TimestampServer = "http://timestamp.digicert.com"
)

# Validate parameters
if (-not $PfxPath -and -not $Thumbprint) {
    Write-Host "ERROR: Either -PfxPath or -Thumbprint must be specified" -ForegroundColor Red
    exit 1
}

if ($PfxPath -and -not $Password) {
    Write-Host "ERROR: -Password is required when using -PfxPath" -ForegroundColor Red
    exit 1
}

# Find signtool
$signToolPaths = @(
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.18362.0\x64\signtool.exe"
)

$signTool = $signToolPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $signTool) {
    Write-Host "ERROR: SignTool not found. Please install Windows SDK." -ForegroundColor Red
    Write-Host "Download from: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/" -ForegroundColor Yellow
    exit 1
}

Write-Host "Using SignTool: $signTool" -ForegroundColor Cyan
Write-Host ""

$successCount = 0
$failCount = 0

foreach ($file in $FilesToSign) {
    if (-not (Test-Path $file)) {
        Write-Host "SKIP: File not found: $file" -ForegroundColor Yellow
        continue
    }

    Write-Host "Signing: $file" -ForegroundColor Cyan

    try {
        if ($PfxPath) {
            # PFX-based signing
            $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
            $PlainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

            $signArgs = @(
                "sign",
                "/f", $PfxPath,
                "/p", $PlainPassword,
                "/fd", "SHA256",
                "/tr", $TimestampServer,
                "/td", "SHA256",
                "/v",
                $file
            )

            & $signTool @signArgs

            # Clear password from memory
            [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
        }
        else {
            # Certificate store-based signing
            $signArgs = @(
                "sign",
                "/sha1", $Thumbprint,
                "/fd", "SHA256",
                "/tr", $TimestampServer,
                "/td", "SHA256",
                "/v",
                $file
            )

            & $signTool @signArgs
        }

        if ($LASTEXITCODE -eq 0) {
            Write-Host "  SUCCESS" -ForegroundColor Green
            $successCount++
        }
        else {
            Write-Host "  FAILED (exit code: $LASTEXITCODE)" -ForegroundColor Red
            $failCount++
        }
    }
    catch {
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }

    Write-Host ""
}

Write-Host "=" * 40 -ForegroundColor Cyan
Write-Host "Signing complete: $successCount succeeded, $failCount failed" -ForegroundColor $(if ($failCount -gt 0) { "Yellow" } else { "Green" })

if ($failCount -gt 0) {
    exit 1
}
