<#
.SYNOPSIS
    Verifies the Easy Auth-protected ManagedIdentityWebApi endpoint for system- and user-assigned MI.

.DESCRIPTION
    Acquires an app-only Entra token as the Easy Auth app registration using the LabAuth client
    certificate (certificate-based client credentials), then calls the protected endpoint with a
    bearer token for both system-assigned and (optionally) user-assigned managed identity, asserting
    the web app successfully acquired a managed-identity token. Easy Auth rejects unauthenticated
    callers with HTTP 401.

    Token version note: the resource app registration uses the default requestedAccessTokenVersion
    (v1), so requesting 'api://<clientId>/.default' from the v2.0 token endpoint still yields a
    v1-form access token (iss = https://sts.windows.net/<tenantId>/, aud = api://<clientId>), which
    matches the Easy Auth issuer/audience configuration.

.PARAMETER ClientId
    App (client) ID of the Easy Auth app registration.

.PARAMETER TenantId
    Tenant (directory) ID that hosts the app registration and web app.

.PARAMETER WebAppName
    Name of the App Service (without the .azurewebsites.net suffix).

.PARAMETER ResourceUri
    Resource URI the web app should acquire a managed-identity token for (for example
    https://vault.azure.net).

.PARAMETER UserAssignedClientId
    Optional client ID of a user-assigned managed identity to additionally verify. When empty, only
    the system-assigned identity is checked.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ClientId,

    [Parameter(Mandatory = $true)]
    [string] $TenantId,

    [Parameter(Mandatory = $true)]
    [string] $WebAppName,

    [Parameter(Mandatory = $true)]
    [string] $ResourceUri,

    [string] $UserAssignedClientId
)

$ErrorActionPreference = 'Stop'

$pfxPath = $env:LABAUTH_PFX_PATH
if ([string]::IsNullOrWhiteSpace($pfxPath)) {
    throw "The 'LABAUTH_PFX_PATH' environment variable is empty. It should be published by the 'Install LabAuth client certificate' step."
}

function ConvertTo-Base64Url {
    param([byte[]] $Bytes)
    return [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

# --- Acquire an app-only token as the Easy Auth app using the LabAuth certificate. ---
$cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
    $pfxPath, '', [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)

$tokenEndpoint = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token"
$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

$header = @{ alg = 'RS256'; typ = 'JWT'; x5t = (ConvertTo-Base64Url $cert.GetCertHash()) } | ConvertTo-Json -Compress
$payload = @{
    aud = $tokenEndpoint
    iss = $ClientId
    sub = $ClientId
    jti = [guid]::NewGuid().ToString()
    nbf = $now
    exp = $now + 600
} | ConvertTo-Json -Compress

$unsigned = (ConvertTo-Base64Url ([System.Text.Encoding]::UTF8.GetBytes($header))) + '.' +
            (ConvertTo-Base64Url ([System.Text.Encoding]::UTF8.GetBytes($payload)))

$rsa = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)
$signature = $rsa.SignData(
    [System.Text.Encoding]::UTF8.GetBytes($unsigned),
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
$clientAssertion = $unsigned + '.' + (ConvertTo-Base64Url $signature)

$tokenRequestBody = @{
    client_id             = $ClientId
    scope                 = "api://$ClientId/.default"
    grant_type            = 'client_credentials'
    client_assertion_type = 'urn:ietf:params:oauth:client-assertion-type:jwt-bearer'
    client_assertion      = $clientAssertion
}

$tokenResponse = Invoke-RestMethod -Uri $tokenEndpoint -Method Post -Body $tokenRequestBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
if ([string]::IsNullOrEmpty($accessToken)) {
    throw 'Failed to acquire an access token for the Easy Auth app registration.'
}

# --- Call the protected endpoint for each managed identity type. ---
$baseUrl = "https://$WebAppName.azurewebsites.net/AppService?resourceuri=$ResourceUri"
$authHeaders = @{ Authorization = "Bearer $accessToken" }

function Invoke-MiEndpoint {
    param(
        [string] $Label,
        [string] $Url
    )

    Write-Host "[$Label] Calling: $Url"
    $response = $null
    $lastError = $null

    # Allow the freshly deployed app to warm up; retry a few times.
    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            $response = Invoke-RestMethod -Uri $Url -Method Get -Headers $authHeaders -TimeoutSec 120
            break
        }
        catch {
            $lastError = $_.Exception.Message
            Write-Host "[$Label] attempt $attempt failed: $lastError"
            Start-Sleep -Seconds 15
        }
    }

    if ($null -eq $response) {
        throw "[$Label] All attempts to call the protected endpoint failed. Last error: $lastError"
    }

    Write-Host "[$Label] Response: $response"
    if ("$response" -notmatch 'Access token received') {
        throw "[$Label] Unexpected response from protected endpoint: $response"
    }

    Write-Host "[$Label] OK"
}

# System-assigned managed identity (no userAssignedId).
Invoke-MiEndpoint -Label 'SAMI' -Url $baseUrl

# User-assigned managed identity (when a client ID is provided).
if (-not [string]::IsNullOrWhiteSpace($UserAssignedClientId)) {
    Invoke-MiEndpoint -Label 'UAMI' -Url ($baseUrl + '&userAssignedId=' + $UserAssignedClientId)
}
else {
    Write-Host 'UserAssignedClientId not provided; skipping user-assigned managed identity check.'
}

Write-Host 'All managed identity checks passed.'
