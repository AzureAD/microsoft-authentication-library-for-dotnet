# Certificate Credential Setup

## Overview
Certificates are used to authenticate confidential clients in MSAL.NET. This guide covers setup and usage.

## Loading Certificates

### From File
```csharp
var cert = new X509Certificate2("path/to/certificate.pfx", "password");
```

### From Certificate Store
```csharp
// Load from Windows certificate store
var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
store.Open(OpenFlags.ReadOnly);
var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
var cert = certs[0];
store.Close();
```

### From Azure Key Vault
```csharp
var kvUri = $"https://{vaultName}.vault.azure.net";
var client = new CertificateClient(new Uri(kvUri), new DefaultAzureCredential());
var cert = await client.DownloadCertificateAsync(certificateName);
```

## Certificate Requirements
- Must include private key
- Common Name (CN) typically matches application name
- Use strong key sizes (2048-bit minimum recommended)
- Ensure certificate is not expired

## When to Use
- **Authorization Code Flow** - Web applications requiring user sign-in
- **OBO Flow** - Middle-tier services acting on behalf of users
- **Client Credentials** - Daemon applications
