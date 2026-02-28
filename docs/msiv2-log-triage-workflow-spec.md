# Specification: Automated MSI v2 Log Triage Workflow

## Goal
Automatically classify **Managed Identity V2 (mTLS PoP)** failures from **MSAL logs** posted in GitHub issues, provide structured diagnosis, and apply relevant labels—reducing manual triage burden.

---

## Core Components

### 1) GitHub Actions Workflow (`.github/workflows/mi-v2-log-triage.yml`)
- **Triggers:** Issue opened, edited, or labeled
- **Gating:** Only processes issues containing MSI v2 markers or `ManagedIdentityClient` context
- **Output:** Posts/updates a single **idempotent comment** (keyed by a hidden HTML marker) + applies `mi-v2:*` diagnostic labels
- **Security:** All user input flows through environment variables (no template injection)

### 2) Log Analyzer Engine (`.github/msal-log-triage/`)
Core Node.js analyzer that:
- Extracts logs from markdown code blocks (issue body + last 5 comments, capped at **250k chars**)
- Redacts JWT tokens (`xxxxx.yyyyy.zzzzz` → `<redacted-jwt>`) while preserving correlation IDs
- Parses structured log lines: `[HttpManager]`, `[CertCache]`, `[PersistentCert]`, `[ImdsV2]`
- Detects which of **6 MSI v2 pipeline stages** are present
- Classifies failures against rule-based signatures (**confidence:** high/medium/low)

### 3) Pipeline Stages Detected

| Stage | Evidence |
|---|---|
| `MIv2.KeyGuard.KeyCreation` | KeyGuard errors, `mtls_pop_requires_keyguard` |
| `MIv2.IMDS.GetPlatformMetadata` | `/metadata/identity/getplatformmetadata` request |
| `MIv2.MAA.Attestation` | Attestation token provider or endpoint |
| `MIv2.IMDS.IssueCredential` | `/metadata/identity/issuecredential` request |
| `MIv2.Cache.ReuseOrMintCert` | `[CertCache]` HIT/MISS, `[PersistentCert]` logs |
| `MIv2.STS.mTLS.Token` | `/oauth2/v2.0/token` + `Binding Certificate: True` |

### 4) Failure Classification Rules (`.github/msal-log-triage/signatures.yml`)

#### mTLS + TLS/SCHANNEL failure → `MIv2.STS.mTLS.Token` (high)
- **Signature:** `Binding Certificate: True` + (SslStream / SocketException(10054) / SCHANNEL)

#### IMDS metadata failure → `MIv2.IMDS.GetPlatformMetadata` (high)

#### IMDS credential failure → `MIv2.IMDS.IssueCredential` (high)

#### Attestation failure/missing → `MIv2.MAA.Attestation` (medium/low)

#### KeyGuard unavailable → `MIv2.KeyGuard.KeyCreation` (high)

### 5) Output Format
Structured comment with:
- Detected flow (e.g., "Managed Identity V2 (mTLS PoP)")
- Most likely failing stage + confidence level
- Evidence (parsed log signatures, **NOT** raw log echoes)
- What this means (explanation of failure mode)
- Next triage steps (actionable diagnostics)

 Manual CLI verification with sample logs

---

## Key Benefits
- ✅ Reduces manual triage time for MSI v2 failures
- ✅ Consistent, rule-based classification
- ✅ Preserves issue context without exposing sensitive logs
- ✅ Zero impact on MSAL library code (workflow-only automation)
