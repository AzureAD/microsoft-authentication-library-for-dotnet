# Troubleshooting Guide

## Certificate Issues

### "Certificate not found"
**Symptoms:** MsalClientException with certificate-related error

**Solutions:**
1. Verify certificate file path and permissions
2. Check certificate is not expired
3. Ensure certificate includes private key
4. Verify certificate loaded correctly:
```csharp
var cert = new X509Certificate2("path/to/cert.pfx", "password");
Console.WriteLine($"Thumbprint: {cert.Thumbprint}");
Console.WriteLine($"Expires: {cert.GetExpirationDateString()}");
Console.WriteLine($"Has private key: {cert.HasPrivateKey}");
```

### "Certificate validation failed"
**Symptoms:** CERTIFICATE_REVOCATION_CHECK_FAILED or similar

**Solutions:**
1. Check certificate chain integrity
2. Verify CA root is trusted
3. Ensure CRL endpoints are accessible

## Managed Identity Issues

### "ManagedIdentityCredential authentication unavailable, no Managed Identity endpoint found"
**Symptoms:** Exception when using ManagedIdentityCredential outside Azure

**Solutions:**
1. Ensure resource is deployed in Azure
2. Verify Managed Identity is enabled
3. Check IMDS endpoint is accessible (port 169.254.169.254:80)

### "The user-assigned identity was not found"
**Symptoms:** ManagedIdentityCredential fails for user-assigned identity

**Solutions:**
1. Verify user-assigned identity exists
2. Confirm correct client ID passed to ManagedIdentityCredential
3. Check identity is assigned to the resource:
```powershell
Get-AzUserAssignedIdentity -ResourceGroupName "RG" -Name "ID"
```

## Authorization Issues

### "AADSTS65001: User or admin has not consented"
**Symptoms:** Token acquisition fails with consent error

**Solutions:**
1. Grant admin consent in Azure Portal
2. Verify application permissions are set correctly
3. Check tenant ID is correct

### "AADSTS700016: Application not found in directory"
**Symptoms:** Application reference issue

**Solutions:**
1. Verify clientId matches Azure AD registration
2. Confirm service principal exists
3. Check for typos in IDs

## SNI Issues

### "InvalidOperation: SNI requires..."
**Symptoms:** WithCertificateWithSNI fails

**Solutions:**
1. Verify certificate validity
2. Check clientId is non-null
3. Ensure Azure AD has SNI configuration enabled

## Network & Connectivity

### "Unable to reach authority endpoint"
**Symptoms:** HttpRequestException or timeout

**Solutions:**
1. Check internet connectivity
2. Verify authority URL is correct
3. Check firewall/proxy settings allow AAD endpoints

## General Debugging

### Enable Logging
```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(cert)
    .WithLogging((level, message, pii) => 
    {
        Debug.WriteLine($"[{level}] {message}");
    }, LogLevel.Verbose, enablePiiLogging: true) // Caution: PII in logs
    .Build();
```

### Check Cache Status
```csharp
// Clear cache if needed
var accounts = (await app.GetAccountsAsync()).ToList();
foreach (var account in accounts)
{
    await app.RemoveAsync(account);
}
```
