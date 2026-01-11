<#
.SYNOPSIS
    Creates a self-signed code signing certificate for development

.DESCRIPTION
    This script creates a self-signed certificate suitable for code signing.
    The certificate is stored in the current user's certificate store and
    exported to a PFX file for use in CI/CD pipelines.

    NOTE: Self-signed certificates will trigger SmartScreen warnings.
    For production, use a commercial code signing certificate.

.PARAMETER CertName
    The common name for the certificate (default: "AgentConnect Development")

.PARAMETER Password
    The password for the exported PFX file (will prompt if not provided)

.PARAMETER OutputPath
    Path where the PFX file will be saved (default: .\agentconnect-dev.pfx)

.PARAMETER ValidYears
    Number of years the certificate will be valid (default: 5)

.EXAMPLE
    .\sign-certificate.ps1
    Creates a certificate with default settings, prompting for password

.EXAMPLE
    .\sign-certificate.ps1 -Password (ConvertTo-SecureString "MyPassword" -AsPlainText -Force) -OutputPath "C:\certs\dev.pfx"
    Creates a certificate with specified password and output path
#>

param(
    [string]$CertName = "AgentConnect Development",
    [SecureString]$Password,
    [string]$OutputPath = ".\agentconnect-dev.pfx",
    [int]$ValidYears = 5
)

# Prompt for password if not provided
if (-not $Password) {
    $Password = Read-Host -Prompt "Enter password for PFX file" -AsSecureString
}

Write-Host ""
Write-Host "Creating self-signed code signing certificate..." -ForegroundColor Cyan
Write-Host "  Name: $CertName"
Write-Host "  Valid for: $ValidYears years"
Write-Host ""

try {
    # Create the certificate
    $cert = New-SelfSignedCertificate `
        -Subject "CN=$CertName, O=AveronHQ, L=Development" `
        -Type CodeSigningCert `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -NotAfter (Get-Date).AddYears($ValidYears) `
        -KeyUsage DigitalSignature `
        -KeyAlgorithm RSA `
        -KeyLength 4096 `
        -HashAlgorithm SHA256

    Write-Host "Certificate created successfully!" -ForegroundColor Green
    Write-Host "  Thumbprint: $($cert.Thumbprint)"
    Write-Host "  Expires: $($cert.NotAfter)"
    Write-Host ""

    # Export to PFX
    $certPath = "Cert:\CurrentUser\My\$($cert.Thumbprint)"
    Export-PfxCertificate -Cert $certPath -FilePath $OutputPath -Password $Password | Out-Null

    $fullPath = (Resolve-Path $OutputPath).Path
    Write-Host "Certificate exported to: $fullPath" -ForegroundColor Green
    Write-Host ""

    # Generate base64 for GitHub secrets
    $base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($fullPath))

    Write-Host "=" * 60 -ForegroundColor Yellow
    Write-Host "GITHUB SECRETS SETUP" -ForegroundColor Yellow
    Write-Host "=" * 60 -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Add these secrets to your GitHub repository:" -ForegroundColor Cyan
    Write-Host "  Settings > Secrets and variables > Actions > New repository secret"
    Write-Host ""
    Write-Host "1. CERT_PASSWORD" -ForegroundColor White
    Write-Host "   Value: (the password you entered above)"
    Write-Host ""
    Write-Host "2. CERT_PFX_BASE64" -ForegroundColor White
    Write-Host "   Value: (copy the base64 string below)"
    Write-Host ""
    Write-Host "Base64-encoded certificate (copy this entire string):" -ForegroundColor Cyan
    Write-Host ""
    Write-Host $base64 -ForegroundColor Gray
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Yellow
    Write-Host ""
    Write-Host "IMPORTANT NOTES:" -ForegroundColor Yellow
    Write-Host "- Keep the PFX file and password secure"
    Write-Host "- Self-signed certs will show SmartScreen warnings"
    Write-Host "- For production, purchase a commercial code signing certificate"
    Write-Host "- See docs/CODE_SIGNING.md for commercial certificate guidance"
    Write-Host ""

    # Return info
    return @{
        Thumbprint = $cert.Thumbprint
        Subject = $cert.Subject
        NotAfter = $cert.NotAfter
        PfxPath = $fullPath
        Base64Length = $base64.Length
    }
}
catch {
    Write-Host "ERROR: Failed to create certificate" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
