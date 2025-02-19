# MSAL MSI V2 /credential Endpoint Design Document

## Overview

This document provides detailed guidance for SDK developers to implement MSI V2 `/credential` endpoint support. It focuses on the **token acquisition process**, ensuring seamless interactions with Managed Identity Resource Providers (MIRPs) on **Azure Virtual Machines (VMs) and Virtual Machine Scale Sets (VMSS)**.

## Goals

The primary objective is to enable seamless token acquisition in MSI V2 for VM/VMSS, utilizing the `/credential` endpoint.

- Define the **MSI V2 token acquisition process**.
- Describe how MSAL interacts with the `/credential` and the ESTS regional token endpoint.
- Ensure compatibility with **Windows and Linux** VMs and VMSS.

## Token Acquisition Process

In **MSI V1**, IMDS or any other Managed Identity Resource Provider (MIRP) directly returns an **access token**. However, in **MSI V2**, the process involves two steps:

### Short-Lived Credential Retrieval from `/credential` Endpoint

- Azure Managed Identity Resource Providers host the `/credential` endpoint.
- The client (MSAL) calls the `/credential` endpoint to retrieve a **short-lived credential (SLC)**.
- This credential is valid for a short duration and must be used promptly in the next step.

### Access Token Acquisition via ESTS

- The client presents the **short-lived credential** to **ESTS** over **MTLS** as an assertion.
- ESTS validates the credential and issues an **access token**.
- The access token is then used to authenticate with Azure services.

## Certificate Handling

To start the flow, MSAL requires a certificate. MSAL follows these steps:

1. **Check for an existing certificate (Windows only)**: MSAL looks for a platform certificate (`devicecert.mtlsauth.local`) in the given Azure resource (In both local machine and local user store). 
2. **Create a new certificate, if none is found**: If a platform certificate is not available, MSAL generates one (self signed) for authentication.
3. **Linux Only**: MSAL will always generate a self signed certificate on Linux.

## Source Detection Logic

MSAL follows a source detection process to determine how to interact with MSI endpoints and acquire tokens.

### Environment Variable Check

MSAL checks for Azure resource type based on specific environment variables to determine if the application is running on:

- **Service Fabric**
- **App Service**
- **Machine Learning**
- **Cloud Shell**
- **Azure Arc**

If identified, MSAL will use the appropriate legacy MSI endpoint for that resource.

### Fallback to IMDS

- If no specific Azure resource is identified from the environment variables, MSAL will fall back to IMDS (VMs and VMSS).
- This fallback is the MSI v1 design or the legacy fallback mechanism.
- In this new MSI v2 design, Before fully falling back to IMDS, MSAL will now **probe for the Credential Endpoint**.
- MSAL probes to see if the `/credential` endpoint exists.
- If the `/credential` endpoint is unavailable, it falls back to the legacy `/token` endpoint.
- If probe is succesful then we can assume the current Azure Resource is a VM/VMSS 

## MSI V2 /credential Endpoint Details

### Short-Lived Credential Retrieval

- The `/credential` endpoint provides a **temporary credential** instead of an access token.
- This credential is only valid for a short duration (1 hour) and must be used **immediately** to acquire an access token from ESTS.
- This mechanism improves security by reducing the lifetime of sensitive authentication materials.

### Retry Logic

MSAL uses the **default Managed Identity [retry](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/651b71c7d1dcaf3261e598e01e017dfd3672bb25/src/client/Microsoft.Identity.Client/Http/HttpManagerFactory.cs#L28) policy** for MSI V2 credential/token requests, whether calling the ESTS endpoint or the new `/credential` endpoint. i.e. MSAL performs 3 retries with a 1 second pause between each retry. Retries are performed on certain error [codes](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/651b71c7d1dcaf3261e598e01e017dfd3672bb25/src/client/Microsoft.Identity.Client/Http/HttpRetryCondition.cs#L12) only.

## Steps for MSI V2 Authentication

This section outlines the necessary steps to acquire an access token using the MSI V2 `/credential` endpoint. 

### 1. Check for an Existing (Platform) Certificate (Windows only)
- Search for a specific certificate (`devicecert.mtlsauth.local`) in `(Cert:\LocalMachine\My)`.
- If the certificate is not found in Local Machine, check Current User's certificate store `(Cert:\CurrentUser\My)`.

### 2. Generate a New Certificate (if platform certificate is not found)
- If no valid platform certificate is found in Cert:\LocalMachine\My or Cert:\CurrentUser\My, create a new in-memory self-signed certificate.
- This applies especially to Linux VMs, where platform certificates are not pre-configured, and MSAL must always generate an in-memory certificate for MTLS authentication.

#### Certificate Creation Requirements
- **Subject Name:** CN=mtls-auth (subject name not final).
- **Validity Period:** 90 days.
- **Key Export Policy:** Private key must be exportable to allow use for MTLS authentication.
- **Key Usage must include:** Digital Signature, Key Encipherment and TLS Client Authentication.
- **Storage:** The certificate should exist only in memory. It is not stored in the certificate store. It is discarded when the process exits.

#### Certificate Rotation Strategy
- **Track Expiry:** The expiration of the certificate must be monitored at runtime.
- **Rotation Trigger:** 5 days before expiry, generate a new in-memory certificate.

### 3. Extract Certificate Data
- Convert the certificate to a Base64-encoded string (`x5c`).
- Format the JSON payload containing the certificate details for request authentication.

### 4. Request MSI Credential
- Send a POST request to the IMDS `/credential` endpoint with the certificate details.
- The request must include:
  - `Metadata: true` header.
  - `X-ms-Client-Request-id` header with a GUID.
  - JSON body containing the certificate's public key in `jwk` format. [RFC](https://datatracker.ietf.org/doc/html/rfc7517#appendix-B) 
- Parse the response to extract:
  - `regional_token_url`
  - `tenant_id`
  - `client_id`
  - `credential` (short-lived credential).

### 5. Request Access Token from ESTS
- Construct the OAuth2 request body, including:
  - `grant_type=client_credentials`
  - `scope=https://management.azure.com/.default`
  - `client_id` from the MSI response.
  - `client_assertion` containing the short-lived credential.
  - `client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer`.
- Send a POST request to the `regional_token_url` with the certificate for mutual TLS (mTLS) authentication.

### 6. Retrieve and Use Access Token
- Parse the response to extract the `access_token`.
- Use the access token to authenticate requests to Azure services.
- Handle any errors that may occur during the token request.

## End-to-End Script

```powershell
# Define certificate subject names
$searchSubject = "CN=devicecert.mtlsauth.local"  # Existing cert to look for
$newCertSubject = "CN=mtls-auth"  # Subject for new self-signed cert

# Step 1: Search for an existing certificate in LocalMachine\My
$cert = Get-ChildItem -Path "Cert:\LocalMachine\My" | Where-Object { $_.Subject -eq $searchSubject -and $_.NotAfter -gt (Get-Date) }

# Step 2: If not found, search in CurrentUser\My
if (-not $cert) {
    Write-Output "🔍 No valid certificate found in LocalMachine\My. Checking CurrentUser\My..."
    $cert = Get-ChildItem -Path "Cert:\CurrentUser\My" | Where-Object { $_.Subject -eq $searchSubject -and $_.NotAfter -gt (Get-Date) }
}

# Step 3: If found, use it
if ($cert) {
    Write-Output "✅ Found valid certificate: $($cert.Subject)"
} else {
    Write-Output "❌ No valid certificate found in both stores. Creating a new self-signed certificate in `CurrentUser\My`..."

    # Step 4: Generate a new self-signed certificate in `CurrentUser\My`
    # For POC we are creating the cert in the user store. But in Product this will be a in-memory cert
    $cert = New-SelfSignedCertificate `
        -Subject $newCertSubject `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyExportPolicy Exportable `
        -KeySpec Signature `
        -KeyUsage DigitalSignature, KeyEncipherment `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
        -NotAfter (Get-Date).AddDays(90)

    Write-Output "✅ Created certificate in CurrentUser\My: $($cert.Thumbprint)"
}

# Ensure `$cert` is valid
if (-not $cert) {
    Write-Error "❌ No certificate found or created. Exiting."
    exit
}

# Step 5: Compute SHA-256 of the Public Key for `kid`
$publicKeyBytes = $cert.GetPublicKey()
$sha256 = New-Object System.Security.Cryptography.SHA256Managed
$certSha256 = [BitConverter]::ToString($sha256.ComputeHash($publicKeyBytes)) -replace "-", ""

Write-Output "🔐 Using SHA-256 Certificate Identifier (kid): $certSha256"

# Step 6: Convert certificate to Base64 for JWT (x5c field)
$x5c = [System.Convert]::ToBase64String($cert.RawData)
Write-Output "📜 x5c: $x5c"

# Step 7: Construct the JSON body properly
$bodyObject = @{
    cnf = @{
        jwk = @{
            kty = "RSA"
            use = "sig"
            alg = "RS256"
            kid = $certSha256  # Use SHA-256 instead of Thumbprint
            x5c = @($x5c)  # Ensures correct array formatting
        }
    }
    latch_key = $false  # Final version of the product should not have this. IMDS team is working on removing this. 
}

# Convert JSON object to a string
$body = $bodyObject | ConvertTo-Json -Depth 10 -Compress
Write-Output "🔹 JSON Payload: $body"

# Step 8: Request MSI credential
$headers = @{
    "Metadata" = "true"
    "X-ms-Client-Request-id" = [guid]::NewGuid().ToString()
}

$imdsResponse = Invoke-WebRequest -Uri "http://169.254.169.254/metadata/identity/credential?cred-api-version=1.0" `
    -Method POST `
    -Headers $headers `
    -Body $body

$jsonContent = $imdsResponse.Content | ConvertFrom-Json

$regionalEndpoint = $jsonContent.regional_token_url + "/" + $jsonContent.tenant_id + "/oauth2/v2.0/token"
Write-Output "✅ Using Regional Endpoint: $regionalEndpoint"

# Step 9: Authenticate with Azure
$tokenHeaders = @{
    "Content-Type" = "application/x-www-form-urlencoded"
    "Accept" = "application/json"
}

$tokenRequestBody = "grant_type=client_credentials&scope=https://management.azure.com/.default&client_id=$($jsonContent.client_id)&client_assertion=$($jsonContent.credential)&client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer"

try {
    $tokenResponse = Invoke-WebRequest -Uri $regionalEndpoint `
        -Method POST `
        -Headers $tokenHeaders `
        -Body $tokenRequestBody `
        -Certificate $cert  # Use the full certificate object

    $tokenJson = $tokenResponse.Content | ConvertFrom-Json
    Write-Output "🔑 Access Token: $($tokenJson.access_token)"
} catch {
    Write-Error "❌ Failed to retrieve access token. Error: $_"
}
```

## Summary of New APIs on Managed Identity Builder

| API Name                         | Purpose                                                                            |
|----------------------------------|------------------------------------------------------------------------------------|
| `GetBindingCertificate()`        | Helper method to get the binding certificate when a credential endpoint exist.     |
| `GetManagedIdentitySourceAsync()`| Helper method to get the managed identity source.                                  |
| `WithCorrelationID(GUID id)`     | Sets the correlation id for the managed identity requests (v2 source only)         |    

## Client-Side Telemetry

To improve observability and diagnostics of Managed Identity (MSI) scenarios within MSAL, we propose introducing a **new telemetry counter** named `MsalMsiCounter`. This counter will be incremented (or otherwise recorded) whenever MSI token acquisition activities occur, capturing the most relevant context in the form of tags.

### Counter Name
- **`MsalMsiCounter`**

### Tags
Each time we increment `MsalMsiCounter`, we include the following tags:

1. **MsiSource**  
   Describes which MSI path or resource is used.  
   - Possible values: `"AppService"`, `"CloudShell"`, `"AzureArc"`, `"ImdsV1"`, `"ImdsV2"`, `"ServiceFabric"`

2. **TokenType**  
   Specifies the type of token being requested or used.  
   - Possible values: `"Bearer"`, `"POP"`, `"mtls_pop"`

3. **bypassCache**  
   Indicates whether the MSAL cache was intentionally bypassed.  
   - Possible values: `"true"`, `"false"`

4. **CertType**  
   Identifies which certificate was used during the MSI V2 flow.  
   - Possible values: `"Platform"`, `"inMemory"`, `"UserProvided"`

5. **CredentialOutcome**  
   If using the `/credential` endpoint (ImdsV2) log the outcome.  
   - Not found
   - Retry Failed
   - Retry Succeeded
   - Success

6. **MsalVersion**  
   The MSAL library version in use.  
   - Example: `"4.51.2"`

7. **Platform**  
   The runtime/OS environment.  
   - Examples: `"net6.0-linux"`, `"net472-windows"`

## Related Documents

- **[SLC Design Document](https://microsoft.sharepoint.com/:w:/t/AzureMSI/EURnTEtFXPlDngpYhCUioqUBvbSUWEX7vZjP0nm8bxUsQA?e=Ejok1n&wdLOR=cE6820299-49AF-4D7A-B7F7-F58D65C232B6)**
- **[MSAL EPIC](https://identitydivision.visualstudio.com/Engineering/_workitems/edit/3027078)**

## Glossary

- **MSAL (Microsoft Authentication Library):** SDK for authentication with Azure AD.
- **IMDS (Instance Metadata Service):** Metadata service for Azure VMs.
- **PoP (Proof of Possession) Token:** Token tied to a specific key.
- **SAMI (System Assigned Managed Identity):** Auto-managed identity for Azure resources.
- **UAMI (User Assigned Managed Identity):** Manually created and assigned identity.

This specification serves as a reference for SDK developers integrating MSI V2 features into MSAL.
