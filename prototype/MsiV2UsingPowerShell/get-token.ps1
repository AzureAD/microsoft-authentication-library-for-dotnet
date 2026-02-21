param(
  # Default scope: Graph
  [string]$Scope = "https://graph.microsoft.com/.default",

  # Default resource: mTLS Graph test endpoint
  [string]$ResourceUrl = "https://mtlstb.graph.microsoft.com/v1.0/applications?`$top=5",

  # Extensive logging (headers, decoded claims, timings, etc.)
  [switch]$VerboseLogging = $true,

  # Print full access token (SENSITIVE!)
  [switch]$ShowFullToken = $false
)

$ErrorActionPreference = 'Stop'

<#
.SYNOPSIS
  MSI v2 + KeyGuard + Attestation + IMDS + ESTS mTLS PoP + mTLS resource call (PowerShell)

  Flow:
    1) KeyGuard: Create an RSA key protected by VBS isolation (Credential Guard/KeyGuard).
    2) IMDS Metadata: GET /metadata/identity/getplatformmetadata (returns clientId, tenantId, cuId, attestationEndpoint).
    3) CSR: Create PKCS#10 CSR signed with RSA-PSS(SHA256) + add OID attribute 1.3.6.1.4.1.311.90.2.10
       whose value is DER UTF8String(JSON(cuId)).
    4) MAA Attestation: Use AttestationClientLib.dll to attest the KeyGuard key, producing a JWT.
    5) IMDS Credential: POST /metadata/identity/issuecredential with { csr, attestation_token }.
       Response includes a certificate (base64 DER), mtls_authentication_endpoint, tenant_id, client_id.
    6) ESTS Token: Use mtls_authentication_endpoint + tenant_id + /oauth2/v2.0/token.
       Perform mutual TLS with the client cert and request client_credentials + token_type=mtls_pop.
    7) Resource call: Verify binding (token cnf.x5t#S256 == SHA256(cert.RawData) base64url),
       then call ResourceUrl over mTLS with Authorization: mtls_pop <token>.

.COMPONENTS (what’s involved)
  - KeyGuard (Credential Guard): Holds private key; TLS uses it for client authentication.
  - MAA (Azure Attestation): Issues attestation JWT over platform/key properties.
  - IMDS: Issues the mTLS certificate and tells you the correct tenant + token endpoint base.
  - ESTS mTLS endpoint: Token issuance over mTLS; returns bound token.
  - Schannel/WinHTTP/.NET TLS: Actually presents the cert in the TLS handshake.

.NOTES
  - Works only on Windows PowerShell 7+ (pwsh). If System.Formats.Asn1 is missing, it falls back to hand DER encoding
    for the single UTF8String(JSON) attribute value.
  - Your Graph call may return 403 until you grant permissions/admin consent — that part is expected.
#>

# --------------------------- Logging helpers ---------------------------

function NowStamp { (Get-Date).ToString("HH:mm:ss.fff") }

function Log-Info    { param([string]$Message); Write-Host "$(NowStamp) [INFO] $Message" -ForegroundColor Cyan }
function Log-Success { param([string]$Message); Write-Host "$(NowStamp) [OK  ] $Message" -ForegroundColor Green }
function Log-Warn    { param([string]$Message); Write-Host "$(NowStamp) [WARN] $Message" -ForegroundColor Yellow }
function Log-Error   { param([string]$Message); Write-Host "$(NowStamp) [ERR ] $Message" -ForegroundColor Red }

function Box {
  param([string]$Title)
  Write-Host ""
  Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor DarkCyan
  Write-Host "║ $($Title.PadRight(58)) ║" -ForegroundColor DarkCyan
  Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor DarkCyan
  Write-Host ""
}

function Dump-IfVerbose {
  param([string]$Label, $Obj)
  if (-not $VerboseLogging) { return }
  Log-Info "${Label}:"
  try {
    if ($Obj -is [string]) {
      Write-Host $Obj
    } else {
      ($Obj | ConvertTo-Json -Depth 80) | Write-Host
    }
  } catch {
    Write-Host ($Obj | Out-String)
  }
}

function New-CorrelationId { [guid]::NewGuid().ToString() }

function Abbrev([string]$s) {
  if ([string]::IsNullOrEmpty($s)) { return "(empty)" }
  if ($s.Length -le 90) { return $s }
  return $s.Substring(0,60) + "..." + $s.Substring($s.Length-20)
}

# --------------------------- Constants ---------------------------

$IMDS_ENDPOINT = "http://169.254.169.254"
$CSR_METADATA_PATH = "/metadata/identity/getplatformmetadata"
$CERTIFICATE_REQUEST_PATH = "/metadata/identity/issuecredential"
$ACQUIRE_ENTRA_TOKEN_PATH = "/oauth2/v2.0/token"
$API_VERSION_QUERY_PARAM = "cred-api-version"
$IMDS_V2_API_VERSION = "2.0"

$RSA_KEY_SIZE = 2048
$IMDS_TIMEOUT_SEC = 10
$ESTS_TIMEOUT_SEC = 30

# Credential Guard / KeyGuard flags (CNG)
$NCRYPT_USE_VIRTUAL_ISOLATION_FLAG = 0x00020000
$NCRYPT_USE_PER_BOOT_KEY_FLAG      = 0x00040000

# Global logger reference to prevent GC during native callbacks
$script:LoggerDelegate = $null

# --------------------------- Native attestation interop ---------------------------

function Define-Types {
  $typeDef = @'
using System;
using System.Runtime.InteropServices;

public enum LogLevel { Error, Warn, Info, Debug }

[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate void LogFunc(IntPtr ctx, string tag, LogLevel lvl, string func, int line, string msg);

[StructLayout(LayoutKind.Sequential)]
public struct AttestationLogInfo {
    public IntPtr Log;
    public IntPtr Ctx;
}

public static class AttestationClientLib {
    [DllImport("AttestationClientLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern int InitAttestationLib(ref AttestationLogInfo info);

    // IntPtr for key handle (NCRYPT_KEY_HANDLE)
    [DllImport("AttestationClientLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern int AttestKeyGuardImportKey(string endpoint, string authToken, string clientPayload, IntPtr keyHandle, out IntPtr token, string clientId);

    [DllImport("AttestationClientLib.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern void FreeAttestationToken(IntPtr token);

    [DllImport("AttestationClientLib.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern void UninitAttestationLib();
}
'@
  try { Add-Type -TypeDefinition $typeDef }
  catch { if ($_.Exception.Message -notmatch "already exists") { throw } }
}

function Setup-DLL {
  $dllPath = @(
    ".\native\AttestationClientLib.dll",
    ".\AttestationClientLib.dll",
    "${env:USERPROFILE}\Downloads\AttestationClientLib.dll"
  ) | Where-Object { Test-Path $_ } | Select-Object -First 1

  if (-not $dllPath) { throw "AttestationClientLib.dll not found (searched .\native, current dir, Downloads)." }

  $dllDir = Split-Path -Parent $dllPath
  $pathEnv = [Environment]::GetEnvironmentVariable("PATH")
  if ($pathEnv -notlike "*$dllDir*") {
    [Environment]::SetEnvironmentVariable("PATH", "$dllDir;$pathEnv")
  }

  if ($VerboseLogging) { Log-Info "Using AttestationClientLib.dll at: $dllPath" }
}

function Create-Logger {
  $loggerBlock = {
    param([IntPtr]$ctx, [string]$tag, [LogLevel]$lvl, [string]$func, [int]$line, [string]$msg)
    Write-Host "[Native:$tag`:$lvl] $func`:$line - $msg" -ForegroundColor DarkGray
  }
  return $loggerBlock -as [LogFunc]
}

# --------------------------- KeyGuard key creation ---------------------------

function New-KeyGuard {
  Log-Info "Step [1/7]: Creating KeyGuard key (Credential Guard isolated RSA)"

  $cngParams = [System.Security.Cryptography.CngKeyCreationParameters]::new()
  $cngParams.Provider = [System.Security.Cryptography.CngProvider]::new("Microsoft Software Key Storage Provider")
  $cngParams.KeyUsage = [System.Security.Cryptography.CngKeyUsages]::AllUsages
  $cngParams.ExportPolicy = [System.Security.Cryptography.CngExportPolicies]::None

  $cngParams.KeyCreationOptions =
    [System.Security.Cryptography.CngKeyCreationOptions]::OverwriteExistingKey `
    -bor $NCRYPT_USE_VIRTUAL_ISOLATION_FLAG `
    -bor $NCRYPT_USE_PER_BOOT_KEY_FLAG

  $cngParams.Parameters.Add(
    [System.Security.Cryptography.CngProperty]::new(
      "Length",
      [System.BitConverter]::GetBytes($RSA_KEY_SIZE),
      [System.Security.Cryptography.CngPropertyOptions]::None
    )
  )

  $cngKey = [System.Security.Cryptography.CngKey]::Create(
    [System.Security.Cryptography.CngAlgorithm]::Rsa,
    "MsalMsiV2Key",
    $cngParams
  )

  $prop = $cngKey.GetProperty("Virtual Iso", [System.Security.Cryptography.CngPropertyOptions]::None)
  if ($prop.GetValue().Length -eq 0) { throw "KeyGuard not protected (Virtual Iso property missing/empty)." }

  Log-Success "KeyGuard key created"
  return [System.Security.Cryptography.RSACng]::new($cngKey)
}

# --------------------------- CSR generation helpers ---------------------------

# DER UTF8String(JSON) encoding fallback for PS 5.1 (no System.Formats.Asn1)
function Encode-DerLengthBytes {
  param([int]$Length)
  if ($Length -lt 128) { return ,([byte]$Length) }

  $bytes = New-Object System.Collections.Generic.List[byte]
  while ($Length -gt 0) {
    $bytes.Insert(0, [byte]($Length -band 0xFF))
    $Length = $Length -shr 8
  }
  $prefix = [byte](0x80 -bor $bytes.Count)
  return ,$prefix + $bytes.ToArray()
}

function Encode-DerUtf8StringBytes {
  param([string]$Value)
  $utf8 = [System.Text.Encoding]::UTF8.GetBytes($Value)
  $len  = Encode-DerLengthBytes -Length $utf8.Length
  return ,([byte]0x0C) + $len + $utf8
}

function New-CSR {
  param(
    [System.Security.Cryptography.RSA]$RsaKey,
    [string]$ClientId,
    [string]$TenantId,
    $Cuid
  )

  Log-Info "Step [3/7]: Generating CSR (SHA256 + RSA-PSS) with CUID OID attribute"

  $subject = [System.Security.Cryptography.X509Certificates.X500DistinguishedName]::new("CN=$ClientId, DC=$TenantId")

  $req = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
    $subject,
    $RsaKey,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pss
  )

  $cuidJson = ($Cuid | ConvertTo-Json -Compress)
  $attrValueDer = $null

  try {
    $writer = [System.Formats.Asn1.AsnWriter]::new([System.Formats.Asn1.AsnEncodingRules]::DER)
    $writer.WriteCharacterString([System.Formats.Asn1.UniversalTagNumber]::UTF8String, $cuidJson)
    $attrValueDer = $writer.Encode()
    Log-Success "CUID encoded via AsnWriter"
  }
  catch {
    $attrValueDer = Encode-DerUtf8StringBytes -Value $cuidJson
    Log-Success "CUID encoded via DER fallback (UTF8String)"
  }

  $oid = "1.3.6.1.4.1.311.90.2.10"
  $asnData = [System.Security.Cryptography.AsnEncodedData]::new($oid, $attrValueDer)
  [void]$req.OtherRequestAttributes.Add($asnData)

  $mi = $req.GetType().GetMethod("CreateSigningRequestPem", [Type[]]@())
  if ($mi) {
    $pem = $req.CreateSigningRequestPem()
    $csrBase64 = ($pem -replace "-----BEGIN CERTIFICATE REQUEST-----", "" `
                      -replace "-----END CERTIFICATE REQUEST-----", "" `
                      -replace "\s+", "").Trim()
    if ($VerboseLogging) { Log-Info "CSR length (base64 chars): $($csrBase64.Length)" }
    Log-Success "CSR generated (PEM)"
    return $csrBase64
  }

  $der = $req.CreateSigningRequest()
  $csrBase64 = ([Convert]::ToBase64String($der)).Trim()
  if ($VerboseLogging) { Log-Info "CSR length (base64 chars): $($csrBase64.Length)" }
  Log-Success "CSR generated (DER fallback)"
  return $csrBase64
}

# --------------------------- Attestation (MAA) ---------------------------

function Get-AttestationToken {
  param(
    [string]$MaaEndpoint,
    [System.Security.Cryptography.RSA]$RsaKey,
    [string]$ClientId
  )

  Log-Info "Step [4/7]: Getting attestation token (KeyGuard → MAA)"

  $script:LoggerDelegate = Create-Logger
  $logInfoStruct = [AttestationLogInfo]::new()
  $logInfoStruct.Log = [System.Runtime.InteropServices.Marshal]::GetFunctionPointerForDelegate($script:LoggerDelegate)
  $logInfoStruct.Ctx = [IntPtr]::Zero

  $initResult = [AttestationClientLib]::InitAttestationLib([ref]$logInfoStruct)
  if ($initResult -ne 0) { throw "AttestationClientLib init failed: $initResult" }

  try {
    $keyHandleObj = $RsaKey.Key.Handle
    if ($keyHandleObj -is [System.Runtime.InteropServices.SafeHandle]) {
      $addedRef = $false
      try {
        $keyHandleObj.DangerousAddRef([ref]$addedRef)
        $keyHandle = $keyHandleObj.DangerousGetHandle()
      } finally {
        if ($addedRef) { $keyHandleObj.DangerousRelease() }
      }
    } else {
      $keyHandle = [IntPtr]$keyHandleObj
    }

    $tokenPtr = [IntPtr]::Zero
    $attestResult = [AttestationClientLib]::AttestKeyGuardImportKey($MaaEndpoint, "", "{}", $keyHandle, [ref]$tokenPtr, $ClientId)
    if ($attestResult -ne 0) { throw "AttestKeyGuardImportKey failed: $attestResult" }
    if ($tokenPtr -eq [IntPtr]::Zero) { throw "Attestation token is null" }

    $token = [System.Runtime.InteropServices.Marshal]::PtrToStringAnsi($tokenPtr)
    [AttestationClientLib]::FreeAttestationToken($tokenPtr)

    if ($VerboseLogging) { Log-Info "Attestation token length: $($token.Length)" }
    Log-Success "Attestation token acquired"
    return $token
  }
  finally {
    [AttestationClientLib]::UninitAttestationLib()
  }
}

# --------------------------- IMDS calls ---------------------------

function Invoke-ImdsGetJson {
  param([string]$Url)

  $corr = New-CorrelationId
  $headers = @{ "Metadata"="true"; "x-ms-client-request-id"=$corr }

  if ($VerboseLogging) {
    Log-Info "IMDS GET: $Url"
    Log-Info "IMDS CorrelationId: $corr"
  }

  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  $resp = Invoke-WebRequest -Uri $Url -Headers $headers -UseBasicParsing -TimeoutSec $IMDS_TIMEOUT_SEC -ErrorAction Stop
  $sw.Stop()

  if ($VerboseLogging) {
    Log-Info "IMDS GET status: $($resp.StatusCode) in $($sw.ElapsedMilliseconds) ms"
    Dump-IfVerbose "IMDS GET response headers" $resp.Headers
  }

  return ($resp.Content | ConvertFrom-Json)
}

function Invoke-ImdsPostJson {
  param([string]$Url, [string]$JsonBody)

  $corr = New-CorrelationId
  $headers = @{ "Metadata"="true"; "x-ms-client-request-id"=$corr }

  if ($VerboseLogging) {
    Log-Info "IMDS POST: $Url"
    Log-Info "IMDS CorrelationId: $corr"
  }

  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  $resp = Invoke-WebRequest -Uri $Url -Method POST -Body $JsonBody -Headers $headers -ContentType 'application/json' -UseBasicParsing -TimeoutSec $ESTS_TIMEOUT_SEC -ErrorAction Stop
  $sw.Stop()

  if ($VerboseLogging) {
    Log-Info "IMDS POST status: $($resp.StatusCode) in $($sw.ElapsedMilliseconds) ms"
    Dump-IfVerbose "IMDS POST response headers" $resp.Headers
  }

  return ($resp.Content | ConvertFrom-Json)
}

function Get-Metadata {
  Log-Info "Step [2/7]: Getting metadata from IMDS"
  $url = "$IMDS_ENDPOINT$CSR_METADATA_PATH`?$API_VERSION_QUERY_PARAM=$IMDS_V2_API_VERSION"
  $metadata = Invoke-ImdsGetJson -Url $url
  if ($VerboseLogging) { Dump-IfVerbose "IMDS metadata (parsed)" $metadata }
  Log-Success "Metadata retrieved"
  return $metadata
}

function Get-Certificate {
  param([string]$Csr, [string]$AttestationToken)

  Log-Info "Step [5/7]: Requesting credential (CSR + attestation) from IMDS"
  $url = "$IMDS_ENDPOINT$CERTIFICATE_REQUEST_PATH`?$API_VERSION_QUERY_PARAM=$IMDS_V2_API_VERSION"
  $json = (@{ csr=$Csr; attestation_token=$AttestationToken } | ConvertTo-Json -Compress)

  $credential = Invoke-ImdsPostJson -Url $url -JsonBody $json
  if ($VerboseLogging) { Dump-IfVerbose "IMDS credential response (parsed)" $credential }
  Log-Success "Certificate received"
  return $credential
}

# --------------------------- Token endpoint derivation (matches MSAL logic) ---------------------------

function Get-TokenEndpointFromCredential {
  param($Credential)

  if ($Credential.PSObject.Properties.Name -contains "token_endpoint" -and -not [string]::IsNullOrWhiteSpace($Credential.token_endpoint)) {
    return [string]$Credential.token_endpoint
  }

  $base = ([string]$Credential.mtls_authentication_endpoint).TrimEnd('/') + "/" + ([string]$Credential.tenant_id).Trim('/')
  return $base + $ACQUIRE_ENTRA_TOKEN_PATH
}

# --------------------------- JWT + Binding helpers ---------------------------

function Convert-Base64UrlToBytes([string]$s) {
  $s = $s.Replace('-', '+').Replace('_', '/')
  switch ($s.Length % 4) { 2 { $s += "==" } 3 { $s += "=" } 0 { } default { } }
  return [Convert]::FromBase64String($s)
}

function Get-JwtPayload([string]$jwt) {
  $parts = $jwt.Split('.')
  if ($parts.Length -lt 2) { throw "Invalid JWT" }
  $bytes = Convert-Base64UrlToBytes $parts[1]
  $json  = [Text.Encoding]::UTF8.GetString($bytes)
  return ($json | ConvertFrom-Json)
}

function Get-CnfX5tS256([string]$jwt) {
  $p = Get-JwtPayload $jwt
  return $p.cnf.'x5t#S256'
}

function Get-CertX5tS256([System.Security.Cryptography.X509Certificates.X509Certificate2]$cert) {
  $hash = [System.Security.Cryptography.SHA256]::Create().ComputeHash($cert.RawData)
  return ([Convert]::ToBase64String($hash)).TrimEnd('=').Replace('+','-').Replace('/','_')
}

# --------------------------- ESTS token acquisition over mTLS ---------------------------

function Get-Token {
  param(
    [string]$TokenEndpoint,
    [string]$ClientId,
    [string]$Scope,
    [System.Security.Cryptography.X509Certificates.X509Certificate2]$Cert
  )

  Log-Info "Step [6/7]: Acquiring access token from ESTS (mTLS PoP)"
  try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch {}

  $body = @{
    grant_type = "client_credentials"
    client_id  = $ClientId
    scope      = $Scope
    token_type = "mtls_pop"
  }

  $form = ($body.GetEnumerator() | ForEach-Object {
    "$($_.Key)=$([System.Web.HttpUtility]::UrlEncode([string]$_.Value))"
  }) -join '&'

  if ($VerboseLogging) {
    Log-Info "Token endpoint: $TokenEndpoint"
    Log-Info "client_id: $ClientId"
    Log-Info "scope: $Scope"
    Log-Info "token_type: mtls_pop"
  }

  $handler = New-Object System.Net.Http.HttpClientHandler
  $handler.ClientCertificateOptions = [System.Net.Http.ClientCertificateOption]::Manual
  [void]$handler.ClientCertificates.Add($Cert)

  $client = New-Object System.Net.Http.HttpClient($handler)
  $client.Timeout = [TimeSpan]::FromSeconds($ESTS_TIMEOUT_SEC)
  $content = New-Object System.Net.Http.StringContent($form, [Text.Encoding]::UTF8, "application/x-www-form-urlencoded")

  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  try {
    $resp = $client.PostAsync($TokenEndpoint, $content).GetAwaiter().GetResult()
    $text = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $sw.Stop()

    if ($VerboseLogging) {
      Log-Info "Token HTTP status: $([int]$resp.StatusCode) $($resp.ReasonPhrase) in $($sw.ElapsedMilliseconds) ms"
    }

    if (-not $resp.IsSuccessStatusCode) {
      Log-Error "Token request failed: HTTP $([int]$resp.StatusCode) $($resp.ReasonPhrase)"
      throw $text
    }

    $json = $text | ConvertFrom-Json
    $accessToken = $json.access_token

    Log-Success "Access token acquired"

    if ($ShowFullToken) {
      Log-Warn "FULL TOKEN OUTPUT ENABLED (SENSITIVE) — do not share."
      Write-Output $accessToken
    } elseif ($VerboseLogging) {
      Log-Info "Access token length: $($accessToken.Length)"
      Log-Info "Access token (abbrev): $(Abbrev $accessToken)"
    }

    return $accessToken
  }
  finally {
    $client.Dispose()
    $handler.Dispose()
  }
}

# --------------------------- Resource call over mTLS ---------------------------

function Invoke-MtlsResourceCall {
  param(
    [Parameter(Mandatory)] [string] $Url,
    [Parameter(Mandatory)] [System.Security.Cryptography.X509Certificates.X509Certificate2] $Cert,
    [Parameter(Mandatory)] [string] $AccessToken,
    [string] $TokenType = "mtls_pop"
  )

  Log-Info "Step [7/7]: Calling resource over mTLS"

  $cnf = Get-CnfX5tS256 $AccessToken
  $x5t = Get-CertX5tS256 $Cert

  Log-Info "Binding check:"
  Log-Info "  token cnf.x5t#S256 = $cnf"
  Log-Info "  cert  x5t#S256     = $x5t"
  if ($cnf -ne $x5t) { throw "Token is NOT bound to the provided certificate." }
  Log-Success "Bound token ↔ cert verified"

  if ($VerboseLogging) {
    $claims = Get-JwtPayload $AccessToken
    Dump-IfVerbose "Token claims (decoded payload)" $claims
  }

  try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch {}

  $handler = New-Object System.Net.Http.HttpClientHandler
  $handler.ClientCertificateOptions = [System.Net.Http.ClientCertificateOption]::Manual
  [void]$handler.ClientCertificates.Add($Cert)

  $client = New-Object System.Net.Http.HttpClient($handler)
  $client.Timeout = [TimeSpan]::FromSeconds(30)

  $req = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Get, $Url)
  $req.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue($TokenType, $AccessToken)
  $req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new("application/json"))

  if ($VerboseLogging) {
    Log-Info "Resource URL: $Url"
    Log-Info "Authorization: $TokenType <token>"
    Log-Info "Client cert subject: $($Cert.Subject)"
    Log-Info "Client cert thumbprint: $($Cert.Thumbprint)"
  }

  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  try {
    $resp = $client.SendAsync($req).GetAwaiter().GetResult()
    $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $sw.Stop()

    if ($VerboseLogging) {
      Log-Info "Resource HTTP status: $([int]$resp.StatusCode) $($resp.ReasonPhrase) in $($sw.ElapsedMilliseconds) ms"
    }

    if (-not $resp.IsSuccessStatusCode) {
      Log-Error "Resource call failed: HTTP $([int]$resp.StatusCode) $($resp.ReasonPhrase)"
      throw $body
    }

    Log-Success "Resource call succeeded"
    return $body
  }
  finally {
    $client.Dispose()
    $handler.Dispose()
  }
}

# --------------------------- Main ---------------------------

try {
  Box "MSI v2 with KeyGuard: Token + mTLS Resource Call (Verbose)"

  Setup-DLL
  Define-Types

  $overall = [System.Diagnostics.Stopwatch]::StartNew()

  # 1) KeyGuard RSA key
  $rsa = New-KeyGuard

  # 2) Metadata from IMDS
  $metadata = Get-Metadata

  # 3) CSR with OID(CUID JSON)
  $csr = New-CSR -RsaKey $rsa -ClientId $metadata.clientId -TenantId $metadata.tenantId -Cuid $metadata.cuId

  # 4) Attestation token (KeyGuard → MAA)
  $attToken = Get-AttestationToken -MaaEndpoint $metadata.attestationEndpoint -RsaKey $rsa -ClientId $metadata.clientId

  # 5) Issue credential (IMDS)
  $credential = Get-Certificate -Csr $csr -AttestationToken $attToken

  # Bind KeyGuard key to issued cert so Schannel can present it
  $certBytes = [Convert]::FromBase64String($credential.certificate)
  $certPublicOnly = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
    $certBytes,
    $null,
    [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::DefaultKeySet
  )
  $cert = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::CopyWithPrivateKey($certPublicOnly, $rsa)

  Log-Info "Issued cert subject: $($cert.Subject)"
  Log-Info "Issued cert thumbprint: $($cert.Thumbprint)"
  Log-Info "HasPrivateKey: $($cert.HasPrivateKey)"

  # Use canonical from IMDS credential response
  $clientIdForToken = [string]$credential.client_id
  $tenantIdForToken = [string]$credential.tenant_id
  $tokenEndpoint    = Get-TokenEndpointFromCredential -Credential $credential

  Log-Info "Token endpoint: $tokenEndpoint"
  Log-Info "TenantId used : $tenantIdForToken"
  Log-Info "ClientId used : $clientIdForToken"
  Log-Info "Scope         : $Scope"

  # 6) Token call to ESTS mTLS endpoint
  $token = Get-Token -TokenEndpoint $tokenEndpoint -ClientId $clientIdForToken -Scope $Scope -Cert $cert

  Log-Success "✓ SUCCESS - TOKEN ACQUIRED"

  if ($ShowFullToken) {
    Log-Warn "Printing full access token (SENSITIVE):"
    Write-Output $token
  }

  # 7) Resource call using token + cert
  Log-Info "Calling resource: $ResourceUrl"
  $resourceBody = Invoke-MtlsResourceCall -Url $ResourceUrl -Cert $cert -AccessToken $token -TokenType "mtls_pop"

  Log-Success "✓ SUCCESS - RESOURCE CALLED"
  Write-Host ""
  Write-Output $resourceBody

  $overall.Stop()
  Log-Info "Total runtime: $($overall.ElapsedMilliseconds) ms"
}
catch {
  Log-Error "$_"
  exit 1
}
