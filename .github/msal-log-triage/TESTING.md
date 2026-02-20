# MSAL MSI v2 Log Triage — Testing Guide

This document explains how to run tests, validate the analyzer, manually test the workflow, and verify PII redaction.

---

## Unit Tests (Local)

### Prerequisites

- Node.js 18+
- Run from the `.github/msal-log-triage/` directory

### Setup

```bash
cd .github/msal-log-triage
npm install
```

### Run Tests

```bash
npm test
```

Expected output: all tests pass (13+ test cases covering the main scenarios).

### Test Coverage

| Test | Scenario |
|------|----------|
| #5755 mTLS+SCHANNEL | `issue-5755-mtls-schannel.log` — Binding Certificate: True + SocketException(10054) |
| Cache HIT | `[CertCache] HIT` line parsing |
| Cache MISS | `[CertCache] MISS` line parsing |
| Persistent cert reuse | `[PersistentCert] Reused certificate` detection |
| mTLS cache HIT | `mTLS binding cache HIT (memory/persistent)` |
| HttpManager parsing | method, URI, Binding Certificate flag, status code |
| PII redaction | JWT tokens masked, correlation IDs preserved |
| Non-MSI v2 logs | Public client logs produce no comment |
| Gating logic | Various marker/label combinations |
| IMDS failure | `getplatformmetadata` 404 → `MIv2.IMDS.GetPlatformMetadata` |
| Cache log file | Multiple cache operations from `cache-hit-miss.log` |
| extractCodeBlocks | Markdown code block extraction |
| Comment format | Marker, header, next steps present |

---

## Manual Analyzer Test (CLI)

```bash
cd .github/msal-log-triage
npm install
node index.js --input testdata/issue-5755-mtls-schannel.log
```

Expected output:
```
--- Triage Comment ---
## MSAL MSI v2 automated log triage (best-effort)
...
**Most likely failing stage:** `MIv2.STS.mTLS.Token`
**Confidence:** High
...
<!-- msal-log-triage: v1 hash=<md5> -->
--- End Comment ---
Stage: MIv2.STS.mTLS.Token
Confidence: high
```

Try other test files:
```bash
node index.js --input testdata/imds-failure.log
node index.js --input testdata/cache-hit-miss.log
node index.js --input testdata/non-miv2.log   # Should output: No MSI v2 issue detected
```

---

## Validating signatures.yml

```bash
cd .github/msal-log-triage
node -e "
const YAML = require('yaml');
const fs = require('fs');
const rules = YAML.parse(fs.readFileSync('signatures.yml', 'utf8')).rules;
rules.forEach(r => {
  ['id','stage','confidence','conditions','nextSteps','whatItMeans'].forEach(f => {
    if (!r[f]) throw new Error('Rule ' + r.id + ' missing field: ' + f);
  });
});
console.log('All', rules.length, 'rules are valid');
"
```

---

## Manual Workflow Test (GitHub Actions)

### Steps

1. **Create a test issue** in the repository with a title like:
   `[TEST] MSI v2 mTLS failure - automated triage test`

2. **Include MSAL logs** in a code block in the issue body:

   ````markdown
   ## Description
   Getting an error with Managed Identity V2.
   
   ```
   [MSAL] ManagedIdentityClient created for MSI v2 (mTLS PoP flow)
   [ImdsV2] GET https://169.254.169.254/metadata/identity/getplatformmetadata
   [ImdsV2] Response 200
   [CertCache] MISS - no valid certificate found
   [ImdsV2] POST https://169.254.169.254/metadata/identity/issuecredential
   [ImdsV2] Response 200
   [CertCache] SET - certificate stored
   [HttpManager] POST https://login.microsoftonline.com/common/oauth2/v2.0/token
   [HttpManager] Binding Certificate: True
   [MSAL] System.Net.Sockets.SocketException(10054): An existing connection was forcibly closed
   [MSAL] SslStream threw exception during TLS handshake
   ```
   ````

3. **Check Actions tab** — The `MSI v2 Log Triage` workflow should run.

4. **Verify comment** is posted on the issue with:
   - Stage: `MIv2.STS.mTLS.Token`
   - Confidence: High
   - Evidence section (no raw log lines)
   - Next steps mentioning `PersistentCert`, `KeyGuard`, `MSAL_MI_DISABLE_PERSISTENT_CERT_CACHE`
   - Hidden marker `<!-- msal-log-triage: v1 hash=... -->`

5. **Test update behavior** — Edit the issue body slightly (add/remove a line). Verify:
   - Workflow runs again
   - **Same comment is updated** (no duplicate comments)

6. **Test non-MSI v2 issue** — Create an issue with public client logs only (no `ManagedIdentityClient` or `ImdsV2` references). Verify no comment is posted.

### Expected Labels

After triage, these labels may be applied based on detected stage:
- `mi-v2:mtls` — STS mTLS token failure
- `mi-v2:imds` — IMDS endpoint failure
- `mi-v2:attestation` — MAA attestation failure
- `mi-v2:cert-cache` — Certificate cache operation
- `mi-v2:keyguard` — KeyGuard key creation failure

---

## Verifying PII Redaction

Run the analyzer on a log containing a JWT token:

```bash
cd .github/msal-log-triage
node -e "
const { redactPii } = require('./index');
const input = 'token: eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxMjM0In0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';
const result = redactPii(input);
console.log(result);
console.assert(!result.includes('eyJ'), 'JWT should be redacted');
console.log('PII redaction OK');
"
```

Verify:
- JWT tokens appear as `<redacted-jwt>` in the output
- Correlation IDs (GUIDs in `correlation_id` context) are **preserved**
- No raw JWT content in the GitHub comment

---

## Debugging Workflow Failures

1. Go to **Actions** tab in GitHub
2. Select the `MSI v2 Log Triage` workflow run
3. Expand the `Run log triage analyzer` step
4. Check for:
   - `Gating: issue is not an MSI v2 issue with MSAL logs. Skipping.` — Issue doesn't have MSI v2 markers
   - JavaScript errors — Check `index.js` for syntax/logic issues
5. Verify gating conditions:
   - Issue body contains `ManagedIdentityClient`, `ImdsV2`, `issuecredential`, `getplatformmetadata`, `mTLS`, or `mtls_pop`
   - OR issue has label `scenario:ManagedIdentity` or `mi-v2:*`
   - AND log text is non-empty

---

## Diagnostic Environment Variables

These environment variables can help with MSI v2 diagnostics:

| Variable | Effect |
|----------|--------|
| `MSAL_MI_DISABLE_PERSISTENT_CERT_CACHE=true` | Disables persistent cert cache, forces fresh cert issuance |

---

## File Reference

| File | Purpose |
|------|---------|
| `index.js` | Main analyzer engine |
| `signatures.yml` | Failure classification rules |
| `package.json` | Node.js dependencies |
| `__tests__/analyzer.test.js` | Unit tests |
| `testdata/issue-5755-mtls-schannel.log` | mTLS+SCHANNEL failure sample |
| `testdata/cache-hit-miss.log` | Cache HIT/MISS/SET scenarios |
| `testdata/imds-failure.log` | IMDS 404 failure sample |
| `testdata/non-miv2.log` | Non-MSI v2 log (should not trigger) |
| `../../workflows/mi-v2-log-triage.yml` | GitHub Actions workflow |
