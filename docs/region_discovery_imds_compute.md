# Region Discovery via IMDS `/compute` Endpoint Specification

## Overview

This document specifies the migration of MSAL.NET region auto-discovery from
the legacy IMDS text endpoint
`/metadata/instance/compute/location` (api-version `2020-06-01`,
`format=text`) to the newer JSON endpoint `/metadata/instance/compute`
(api-version `2021-02-01`). MSAL parses the `location` field from the JSON
response.

There is **no public API change**. All surrounding behavior — `REGION_NAME`
environment-variable precedence, process-wide caching, retry policy,
timeouts, telemetry, and fallback to the global endpoint — is preserved.

Tracking issue: [#6039](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/6039).

---

## Motivation

- **Reliability.** The Azure IMDS team has indicated that the
  `/metadata/instance/compute` endpoint with `api-version=2021-02-01` is
  the newer, more reliable endpoint and is the preferred surface for
  consumers going forward.
- **Cross-component alignment.** MISE is consolidating its region
  validation logic on the same endpoint. Aligning MSAL avoids divergent
  behavior between the SDK and the validation layer.
- **Single source of truth.** Both region *value* (MSAL) and region
  *validation* (MISE) reading from the same IMDS surface reduces the
  surface area for "MSAL says X, MISE says Y" support escalations.
- **Forward compatibility.** `/compute` returns the full instance metadata
  document, so future region-adjacent fields (e.g., zone, physical
  location) can be read without another endpoint migration.

---

## Current Behavior

`Microsoft.Identity.Client.Region.RegionManager` resolves a region in the
following order:

1. `REGION_NAME` environment variable, if set and well-formed.
2. IMDS call to:
   ```
   GET http://169.254.169.254/metadata/instance/compute/location
       ?api-version=2020-06-01&format=text
   Headers:
     Metadata: true
   ```
   The body is the region string (plain text).
3. On `400 Bad Request`, MSAL probes
   `http://169.254.169.254/metadata/instance/compute/location` (no
   api-version) to read `newest-versions` from the IMDS error response,
   then retries with the latest version.
4. On any other failure, MSAL caches the failure process-wide and falls
   back to the global ESTS endpoint.

The result (success or failure) is cached in static fields for the lifetime
of the process.

---

## Proposed Behavior

Replace step 2 with a call to the JSON `/compute` endpoint and parse the
`location` field:

```
GET http://169.254.169.254/metadata/instance/compute?api-version=2021-02-01
Headers:
  Metadata: true
```

The `format=text` query parameter is **removed**; the new endpoint is
JSON-native.

Sample response (abbreviated):

```json
{
  "azEnvironment": "AzurePublicCloud",
  "location": "westus2",
  "name": "myVm",
  "osType": "Linux",
  "resourceGroupName": "myRg",
  "subscriptionId": "00000000-0000-0000-0000-000000000000",
  "vmId": "11111111-1111-1111-1111-111111111111",
  "vmSize": "Standard_DS2_v2"
}
```

MSAL deserializes the response, reads `location`, normalizes it (lowercase,
strip spaces — same as today), and validates it via the existing
`ValidateRegion` helper. On success, the region is cached as
`RegionAutodetectionSource.Imds`. On failure, the failure is cached and
MSAL falls back to the global endpoint.

The api-version drift handler is preserved unchanged (see *API-Version
Fallback* below), but it now probes the new endpoint URL.

---

## Discovery Order (Normative)

The discovery order **MUST** be:

1. **`REGION_NAME` environment variable** — wins if set and validates.
2. **IMDS `/compute` JSON call** — `api-version=2021-02-01`.
3. **API-version probe** — only if step 2 returns `400 Bad Request`. Probe
   `/metadata/instance/compute` (no api-version) to read
   `newest-versions[0]` from the error body, then retry step 2 with that
   version.
4. **Failure** — cache failure process-wide, return `null`, caller falls
   back to global ESTS.

Cache hit short-circuits all of the above, as it does today.

---

## Request and Response Contract

### Request

| Field         | Value                                                        |
|---------------|--------------------------------------------------------------|
| Method        | `GET`                                                        |
| URL           | `http://169.254.169.254/metadata/instance/compute`           |
| Query         | `api-version=2021-02-01`                                     |
| Headers       | `Metadata: true`                                             |
| Body          | (none)                                                       |
| TLS           | None (link-local IMDS endpoint, by design)                   |
| mTLS cert     | `null`                                                       |
| Timeout       | `_imdsCallTimeoutMs` (default 2000 ms; unchanged)            |
| Retry policy  | `RegionDiscoveryRetryPolicy` (unchanged)                     |

### Response

- **`200 OK`** with a JSON body — MSAL deserializes and reads `location`.
- **`400 Bad Request`** — triggers the api-version probe path.
- **Empty body** or **`location` missing/empty** — treated as
  `FailedAutoDiscovery` (same as today's empty-text-body behavior).
- **Malformed JSON** — treated as `FailedAutoDiscovery`. The exception is
  logged and stored in `RegionDiscoveryFailureReason`.
- **Other status codes / network errors / timeouts** — treated as
  `FailedAutoDiscovery` (unchanged behavior).

### Validation

The extracted `location` value is validated by the existing
`ValidateRegion` helper:

- Non-empty after trim/lowercase.
- Yields a well-formed URI when interpolated into
  `https://{region}.login.microsoft.com`.

No new validation rules are added.

---

## API-Version Fallback

If the pinned api-version `2021-02-01` is ever deprecated by IMDS, the
existing fallback handles it:

1. `/compute?api-version=2021-02-01` returns `400 Bad Request`.
2. MSAL calls `/compute` with no `api-version` query parameter.
3. IMDS responds `400 Bad Request` with a JSON error body containing
   `newest-versions`.
4. MSAL retries `/compute?api-version={newest-versions[0]}`.

This logic is already implemented in
`RegionManager.GetImdsUriApiVersionAsync` and only the base URL needs to
change (from `/compute/location` to `/compute`). The error-response shape
(`LocalImdsErrorResponse`) is identical between the two endpoints.

---

## Caching

Unchanged. Process-wide static fields cache:

- The discovered region on success.
- The failure on failure (so subsequent calls don't repeatedly hammer
  IMDS within the process lifetime).

Both are reset only via `RegionManager.ResetStaticCacheForTest` (test-only
hook) or process restart.

---

## Retry and Timeouts

Unchanged.

- `IRetryPolicyFactory.GetRetryPolicy(RequestType.RegionDiscovery)` is
  reused as-is.
- `_imdsCallTimeoutMs` (default 2000 ms) is reused as-is and applied via
  the same `CancellationTokenSource` linking pattern.

---

## Telemetry

Unchanged.

- `ApiEvent.RegionAutodetectionSource` continues to use
  `RegionAutodetectionSource.Imds` on IMDS-sourced success.
- `ApiEvent.RegionUsed`, `ApiEvent.AutoDetectedRegion`,
  `ApiEvent.RegionOutcome`, and `ApiEvent.RegionDiscoveryFailureReason`
  retain their current semantics.
- The failure-reason string is built from `imdsUri.AbsoluteUri`, so the
  new endpoint is automatically reflected in observability data without
  any telemetry-pipeline changes.

---

## Backward Compatibility

- **No public API change.** No additions or removals in
  `PublicAPI.Unshipped.txt`.
- **No configuration change.** `WithAzureRegion(...)` and
  `ConfidentialClientApplication.AttemptRegionDiscovery` continue to work
  identically.
- **Behavior change is limited** to:
  - The IMDS endpoint URL.
  - The pinned api-version.
  - The response parser (text → JSON deserialization of `location`).
- **Caller-visible result is identical** for any environment where the
  legacy endpoint already worked: same region string, same fallback,
  same telemetry.

---

## Design Decisions

These decisions are baked into the spec; they are not open questions.

### 1. No kill-switch / feature flag for rollback

We do **not** introduce an env var or builder option to revert to the
legacy endpoint. Rationale:

- MSAL has not historically gated low-level IMDS endpoint changes behind
  flags (e.g., the api-version probe was rolled out unconditionally).
- The api-version probe already self-heals against IMDS-side version
  drift, removing the most realistic failure mode.
- A flag means new public surface (or env var), additional code paths,
  ongoing test matrix cost, and an indefinite removal timeline.
- Regressions, if any, are addressed by hotfix release — consistent with
  current MSAL practice for Azure infra dependencies.

### 2. Drop `format=text`

The new endpoint returns JSON natively. `format=text` is meaningful only
for the legacy `/compute/location` text endpoint and is irrelevant on
`/compute`. The request sends only `api-version=2021-02-01` and the
`Metadata: true` header.

### 3. No transitional fallback to `/compute/location`

We do **not** call `/compute/location` as a backup if `/compute` fails.
Rationale:

- Both paths are served by the same IMDS host on the same VM-local
  link-local IP; there is no realistic single-endpoint failure mode.
- A dual-call fallback doubles failure-path latency, doubles the test
  matrix, and creates ambiguity about which endpoint produced a value
  in telemetry.
- The api-version probe already covers the only realistic IMDS-side
  regression (api-version deprecation).

This is a clean cutover.

### 4. Keep the api-version probe on `400 Bad Request`

Preserved unchanged. It is the resilience mechanism that lets us pin
`2021-02-01` today without being stuck on it forever.

### 5. JSON parse / missing-`location` failure handling

Treat as `FailedAutoDiscovery`, log the underlying error, and write the
detail into `RegionDiscoveryFailureReason`. This matches today's behavior
for empty-body responses on the text endpoint.

### 6. Reuse existing `ValidateRegion`

No new validation rules. The extracted `location` runs through the same
well-formed-URI check used today.

### 7. No telemetry schema change

The endpoint URL is already included in failure-reason strings via
`imdsUri.AbsoluteUri`, so observability of the new endpoint is automatic.
Adding a dedicated "endpoint" telemetry field is unnecessary.

---

## Test Plan

Update `tests/Microsoft.Identity.Test.Unit/CoreTests/RegionDiscoveryProviderTests.cs`
and any related mocks. Required scenarios:

| # | Scenario | Expected outcome |
|---|----------|------------------|
| 1 | `REGION_NAME` env var set | Env-var value used, IMDS not called |
| 2 | IMDS `/compute` returns `200` with valid JSON containing `location` | `location` returned; source `Imds` |
| 3 | IMDS `/compute` returns `200` with JSON missing `location` | `FailedAutoDiscovery`, failure cached |
| 4 | IMDS `/compute` returns `200` with empty body | `FailedAutoDiscovery`, failure cached |
| 5 | IMDS `/compute` returns `200` with malformed JSON | `FailedAutoDiscovery`, failure cached |
| 6 | IMDS `/compute` returns `400` → probe returns `newest-versions` → retry succeeds | Region returned; source `Imds` |
| 7 | IMDS `/compute` returns `400` → probe also fails | `FailedAutoDiscovery` |
| 8 | IMDS times out | `FailedAutoDiscovery`, timeout reflected in failure reason |
| 9 | Second call after success | Returned from cache; source `Cache`; no IMDS call |
| 10 | Second call after failure | `FailedAutoDiscovery` from cache; no IMDS call |
| 11 | `location` value with mixed case / spaces | Normalized (lowercase, no spaces) before validation |
| 12 | `location` value that fails `ValidateRegion` | `FailedAutoDiscovery` |

Existing telemetry tests
(`tests/Microsoft.Identity.Test.Unit/TelemetryTests/RegionalTelemetryTests.cs`
and `ClientCredentialWithRegionTests.cs`) must continue to pass without
modification, given the no-schema-change decision.

The integration test
`tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsTests.WithRegion.cs`
runs against a real Azure VM and is the end-to-end gate.

---

## Implementation Notes (non-normative)

- Constant change in `RegionManager`:
  - `ImdsEndpoint`: `http://169.254.169.254/metadata/instance/compute`
  - `DefaultApiVersion`: `2021-02-01`
- `BuildImdsUri` drops the `format=text` parameter.
- Add a small DTO (e.g., `LocalImdsComputeResponse`) with a
  `[JsonPropertyName("location")]` string field; deserialize via the
  existing `JsonHelper.DeserializeFromJson<T>` utility.
- `GetImdsUriApiVersionAsync` continues to use `LocalImdsErrorResponse`
  unchanged.
- Update XML doc / `Verbose` log strings that reference the old endpoint
  text where they appear.

---

## References

- Issue: [#6039 — Consolidate MSAL Region Discovery onto the IMDS `/compute` Endpoint](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/6039)
- IMDS docs: https://learn.microsoft.com/azure/virtual-machines/instance-metadata-service
- Related specs:
  - `docs/imds_retry_based_on_errors.md`
  - `docs/msi_v2/msiv2_revocation_spec.md`
  - `docs/FmiCredential.md`
- Source:
  - `src/client/Microsoft.Identity.Client/Instance/Region/RegionManager.cs`
  - `src/client/Microsoft.Identity.Client/Http/Retry/RegionDiscoveryRetryPolicy.cs`
