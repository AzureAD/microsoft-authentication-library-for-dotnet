# MSAL MSI V2 /credential Endpoint Design Document

## Overview

This document provides detailed guidance for SDK developers to implement MSI V2 `/credential` endpoint support. It focuses on the **token acquisition process**, ensuring seamless interactions with Managed Identity Resource Providers (MIRRPs) on **Azure Virtual Machines (VMs) and Virtual Machine Scale Sets (VMSS)**.

## Goals

The primary objective is to enable seamless token acquisition in MSI V2 for VM/VMSS, utilizing the `/credential` endpoint.

- Define the **MSI V2 token acquisition process**.
- Describe how MSAL interacts with the `/credential` and the ESTS token endpoints.
- Ensure compatibility with **Windows and Linux** VMs and VMSS.

## Token Acquisition Process

In **MSI V1**, IMDS directly returns an **access token**. However, in **MSI V2**, the process involves two steps:

### Short-Lived Credential Retrieval from `/credential` Endpoint

- The client (MSAL) calls the `/credential` endpoint to retrieve a **short-lived credential (SLC)**.
- This credential is valid for a short duration and must be used promptly in the next step.

### Access Token Acquisition via ESTS

- The client presents the **short-lived credential** to **ESTS** over **MTLS**.
- ESTS validates the credential and issues an **access token**.
- The access token is then used to authenticate with Azure services.

## Certificate Handling

To start the flow, MSAL requires a certificate. MSAL follows these steps:

1. **Check for an existing certificate**: MSAL looks for a specific certificate (`devicecert.mtlsauth.local`).
2. **Create a new certificate if not found**: If the expected certificate is not available, MSAL generates one dynamically for authentication.

## Source Detection Logic

MSAL follows a source detection process to determine how to interact with MSI endpoints and acquire tokens.

### Environment Variable Check

MSAL checks for Azure resource type based on specific environment variables to determine if the application is running on:

- **Service Fabric**
- **App Service**
- **Azure Arc**
- **Cloud Shell**
- **Machine Learning**

If identified, MSAL will use the appropriate legacy MSI endpoint for that resource.

### Fallback to IMDS

- If no specific Azure resource is identified from the environment variables, MSAL will fall back to IMDS (VMs and VMSS).
- In this new design, Before fully falling back to IMDS, MSAL will now **probe the Credential Endpoint in IMDS**.
- MSAL probes to see if the `/credential` endpoint exists in IMDS.
- If the `/credential` endpoint is unavailable, it falls back to the legacy `/token` endpoint.

## MSI V2 /credential Endpoint Details

### Short-Lived Credential Retrieval

- The `/credential` endpoint provides a **temporary credential** instead of a direct access token.
- This credential is only valid for a short duration (1 hour) and must be used **immediately** to acquire an access token from ESTS.
- This mechanism improves security by reducing the lifetime of sensitive authentication materials.

## Steps for MSI V2 Authentication

This section outlines the necessary steps to acquire an access token using the MSI V2 `/credential` endpoint. 

### 1. Check for an Existing Certificate
- Search for a valid self-signed certificate in `Cert:\LocalMachine\My`.
- If found, extract its thumbprint and use it for authentication.

### 2. Generate a New Certificate (if not found)
- Create a new self-signed certificate with a 90-day validity.
- Ensure the certificate has:
  - Subject name `CN=mtls-auth` (name not final).
  - Exportable key policy.

### 3. Extract Certificate Data
- Convert the certificate to a Base64-encoded string (`x5c`).
- Format the JSON payload containing the certificate details for request authentication.

### 4. Request MSI Credential
- Send a POST request to the IMDS `/credential` endpoint with the certificate details.
- The request must include:
  - `Metadata: true` header.
  - `X-ms-Client-Request-id` header with a GUID.
  - JSON body containing the certificate's public key in `jwk` format.
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
# Define certificate details
$certSubject = "CN=mtls-auth"
$certThumbprint = ""

# Check for an existing valid certificate in LocalMachine\My
$existingCert = Get-ChildItem -Path "Cert:\LocalMachine\My" | Where-Object { $_.Subject -like "*$certSubject*" -and $_.NotAfter -gt (Get-Date) }

if ($existingCert) {
    Write-Output "✅ Found existing valid certificate: $($existingCert.Subject)"
    $cert = $existingCert
} else {
    Write-Output "❌ No valid certificate found. Creating a new self-signed certificate..."

    # Create a new self-signed certificate
    $cert = New-SelfSignedCertificate `
        -Subject $certSubject `
        -CertStoreLocation "Cert:\LocalMachine\My" `
        -KeyExportPolicy Exportable `
        -KeySpec Signature `
        -KeyUsage DigitalSignature, KeyEncipherment `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
        -NotAfter (Get-Date).AddDays(90)

    Write-Output "✅ Created Self-Signed Certificate: $($cert.Subject)"
}

# Extract the thumbprint
$certThumbprint = $cert.Thumbprint

if (-not $certThumbprint) {
    Write-Error "❌ Certificate thumbprint is empty. Exiting."
    exit
}

Write-Output "🔹 Certificate Thumbprint: $certThumbprint"

# Extract Base64-encoded certificate chain (x5c)
$x5c = [System.Convert]::ToBase64String($cert.RawData)

$jwk = @{
    kty = "RSA"
    use = "sig"
    alg = "RS256"
    kid = $cert.Thumbprint
    x5c = @($x5c)
} | ConvertTo-Json -Depth 10 -Compress

$body = "{""cnf"": {""jwk"": $jwk}}"

Write-Output "🔹 JSON Payload: $body"

# Requesting MSI credential (Step 1)
$headers = @{
    "Metadata" = "true"
    "X-ms-Client-Request-id" = [guid]::NewGuid().ToString()
}

$imdsResponse = Invoke-WebRequest -Uri "http://169.254.169.254/metadata/identity/credential?cred-api-version=1.0" `
    -Method POST `
    -Headers $headers `
    -Body $body `
    -UseBasicParsing

$jsonContent = $imdsResponse.Content | ConvertFrom-Json

$regionalEndpoint = $jsonContent.regional_token_url + "/" + $jsonContent.tenant_id + "/oauth2/v2.0/token"
$clientId = $jsonContent.client_id
$credential = $jsonContent.credential

Write-Output "✅ Using Regional Endpoint: $regionalEndpoint"

$resource = "https://management.azure.com"
$tokenRequestBody = "grant_type=client_credentials&scope=$resource/.default&client_id=$clientId&client_assertion=$credential&client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer"

$tokenHeaders = @{
    "Content-Type" = "application/x-www-form-urlencoded"
    "Accept" = "application/json"
}

try {
    $tokenResponse = Invoke-WebRequest -Uri $regionalEndpoint `
        -Method POST `
        -Headers $tokenHeaders `
        -Body $tokenRequestBody `
        -Certificate $cert `
        -UseBasicParsing

    $tokenJson = $tokenResponse.Content | ConvertFrom-Json
    Write-Output "🔑 Access Token: $($tokenJson.access_token)"
} catch {
    Write-Error "❌ Failed to retrieve access token. Error: $_"
}
```

## Related Documents

- **MSAL SLC Developer Guide.docx**

## Glossary

- **MSAL (Microsoft Authentication Library):** SDK for authentication with Azure AD.
- **IMDS (Instance Metadata Service):** Metadata service for Azure VMs.
- **PoP (Proof of Possession) Token:** Token tied to a specific key.
- **SAMI (System Assigned Managed Identity):** Auto-managed identity for Azure resources.
- **UAMI (User Assigned Managed Identity):** Manually created and assigned identity.

This specification serves as a reference for SDK developers integrating MSI V2 features into MSAL.
