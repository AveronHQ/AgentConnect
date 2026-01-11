# Code Signing for AgentConnect

This document explains how to set up code signing for AgentConnect releases.

## Why Code Signing?

Code signing provides:
- **Trust**: Users know the software comes from a verified publisher
- **Integrity**: Ensures the software hasn't been tampered with
- **SmartScreen**: Reduces or eliminates Windows SmartScreen warnings
- **Enterprise**: Required for many corporate deployments

## Development: Self-Signed Certificate

For development and testing, use a self-signed certificate. Note that self-signed certificates will still trigger SmartScreen warnings on end-user machines.

### Creating a Self-Signed Certificate

1. Open PowerShell as Administrator
2. Navigate to the `build` folder
3. Run the certificate creation script:

```powershell
.\sign-certificate.ps1
```

4. Enter a secure password when prompted
5. The script outputs:
   - PFX file location
   - Base64-encoded certificate for GitHub Secrets
   - Certificate thumbprint

### Setting Up GitHub Secrets

Add these secrets to your repository (Settings > Secrets and variables > Actions):

| Secret Name | Value |
|-------------|-------|
| `CERT_PFX_BASE64` | Base64-encoded PFX file (from script output) |
| `CERT_PASSWORD` | Password you entered when creating the certificate |

### Manual Signing

To sign files locally:

```powershell
.\build\sign-code.ps1 `
    -PfxPath ".\agentconnect-dev.pfx" `
    -Password (ConvertTo-SecureString "your-password" -AsPlainText -Force) `
    -FilesToSign @(".\bin\Release\AgentConnect.exe")
```

## Production: Commercial Certificate

For production releases, purchase a code signing certificate from a trusted Certificate Authority.

### Recommended Certificate Authorities

| Provider | Standard CS | EV CS | Notes |
|----------|-------------|-------|-------|
| [DigiCert](https://www.digicert.com/signing/code-signing-certificates) | ~$474/yr | ~$699/yr | Industry leader |
| [Sectigo](https://sectigo.com/ssl-certificates-tls/code-signing) | ~$299/yr | ~$449/yr | Good value |
| [GlobalSign](https://www.globalsign.com/en/code-signing-certificate) | ~$249/yr | ~$449/yr | Competitive pricing |
| [SSL.com](https://www.ssl.com/certificates/ev-code-signing/) | ~$239/yr | ~$339/yr | Budget-friendly |

*Prices are approximate and subject to change.*

### Standard vs EV Code Signing

| Feature | Standard | EV (Extended Validation) |
|---------|----------|--------------------------|
| SmartScreen Trust | Builds over time | Immediate |
| Validation | Organization verified | Strict identity verification |
| Storage | Software (PFX file) | Hardware token required |
| CI/CD Integration | Easy | Requires special handling |
| Price | Lower | Higher |

**Recommendation**: Start with EV if budget allows - it provides immediate SmartScreen reputation.

### Purchase Process

1. **Choose a CA** from the list above
2. **Select certificate type** (Standard or EV)
3. **Complete validation**:
   - Organization verification (business documents)
   - Domain verification (for organization)
   - Phone verification
4. **For EV certificates**: Receive hardware token (USB)
5. **Download/activate** the certificate

### EV Certificates in CI/CD

EV certificates require a hardware token, which complicates automated signing. Options:

#### Option 1: Azure Trusted Signing (Recommended)

Microsoft's cloud-based signing service. No hardware token needed.

1. Create Azure account
2. Enable Trusted Signing service
3. Upload identity verification
4. Use Azure CLI in GitHub Actions

```yaml
- name: Sign with Azure Trusted Signing
  run: |
    az login --service-principal -u ${{ secrets.AZURE_CLIENT_ID }} -p ${{ secrets.AZURE_CLIENT_SECRET }} --tenant ${{ secrets.AZURE_TENANT_ID }}
    az trustedsigning sign --file AgentConnect.exe --account-name myaccount --profile-name myprofile
```

#### Option 2: SignPath

Cloud signing service that can hold your EV certificate.

1. Create SignPath account
2. Upload certificate or use their HSM
3. Configure GitHub integration
4. Sign via API

#### Option 3: Local Signing Station

Sign releases on a dedicated machine with the hardware token.

1. Build on CI/CD (unsigned)
2. Download artifacts to signing station
3. Sign with hardware token present
4. Upload signed packages to GitHub Release

### Using Commercial Certificate in CI/CD

For standard (non-EV) certificates:

```yaml
# In release.yml
- name: Decode certificate
  run: |
    $certBytes = [Convert]::FromBase64String("${{ secrets.CERT_PFX_BASE64 }}")
    [IO.File]::WriteAllBytes("cert.pfx", $certBytes)

- name: Sign with Velopack
  run: |
    vpk pack ... --signParams "/f cert.pfx /p ${{ secrets.CERT_PASSWORD }} /fd SHA256 /tr http://timestamp.digicert.com /td SHA256"

- name: Cleanup
  if: always()
  run: Remove-Item cert.pfx -Force -ErrorAction SilentlyContinue
```

## Timestamp Servers

Always use a timestamp server when signing. This ensures signatures remain valid after the certificate expires.

| Provider | URL |
|----------|-----|
| DigiCert | `http://timestamp.digicert.com` |
| Sectigo | `http://timestamp.sectigo.com` |
| GlobalSign | `http://timestamp.globalsign.com/tsa/r6advanced1` |
| SSL.com | `http://ts.ssl.com` |

## Troubleshooting

### SmartScreen Warning Still Appears

- **Self-signed**: Expected behavior. Use a commercial certificate for production.
- **New certificate**: Standard certificates need to build reputation. Consider EV.
- **Low download count**: SmartScreen considers download popularity.

### "Publisher Unknown"

- Certificate may not be properly installed
- Check that the certificate chain is complete
- Verify timestamp was applied

### Signing Fails in CI/CD

- Verify `CERT_PFX_BASE64` is properly base64-encoded
- Check `CERT_PASSWORD` is correct
- Ensure Windows SDK is available (for signtool)
- For EV: Hardware token can't be used in cloud CI

## Security Best Practices

1. **Never commit certificates** to source control
2. **Use strong passwords** for PFX files
3. **Rotate certificates** before expiration
4. **Limit access** to signing credentials
5. **Audit signing operations** in CI/CD logs
6. **Consider HSM** for high-security requirements

## Further Reading

- [Microsoft Code Signing Guide](https://docs.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools)
- [Velopack Signing Documentation](https://velopack.io/docs/signing)
- [SmartScreen & Reputation](https://docs.microsoft.com/en-us/windows/security/threat-protection/microsoft-defender-smartscreen/microsoft-defender-smartscreen-overview)
