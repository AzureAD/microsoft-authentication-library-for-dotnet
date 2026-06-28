# MSAL .NET Exception-Content Redaction — Developer Spec

## 1. Context and problem
MSAL .NET surfaces service/server error responses to callers as exceptions. Applications and SDKs routinely catch and log these exceptions. When an exception's message or its raw response body contains sensitive token-like content, that content can appear in application logs or telemetry.

MSAL .NET performs **no** exception-content redaction today. Because MSAL is the common layer that produces these exceptions, it is a natural place to redact MSAL-owned content before the exception is exposed, reducing the risk of accidental exposure in application logs.

## 2. Goals
- A **generic, partner-agnostic** mechanism to redact sensitive token-like content from MSAL-owned exception content produced from service responses.
- A **public extension point** so any application supplies its own scrubber without MSAL code changes.
- An **always-on built-in baseline** that redacts common sensitive token-like content out of the box.
- Redact MSAL-owned message and response-body content on service-derived exceptions at creation.
- **Non-disruptive and cheap**, and **compatible with downstream libraries** that inspect exception content for control flow.

## 3. Non-goals
- Redacting data applications obtain and log on their own, outside MSAL exceptions.
- A configurable rules/policy engine — scrubbing is an opaque content transform supplied by the host.
- Redacting content MSAL must preserve for programmatic recovery (e.g., claims challenges).
- Guaranteeing redaction of arbitrary or host-specific sensitive content inside foreign nested exceptions beyond the best-effort baseline final pass.
- Redacting every MSAL exception type or arbitrary exception field — scope is limited to MSAL-owned service-derived exception content.

## 4. Design principles
- **Separate mechanism from rules.** MSAL owns a generic pipeline and a public hook; specific redaction rules are data, supplied by the built-in baseline and/or hosts.
- **One pipeline.** In-scope MSAL-owned content assignments made while constructing service-derived exceptions flow through a single internal redaction pipeline.
- **Defense in depth.** A built-in baseline always runs; host scrubbers are additive and never weaken or disable it.
- **Degrade to baseline.** A misbehaving host scrubber falls back to the baseline; it never changes or suppresses the error.
- **Redact at the source.** Redact when the service-derived exception is created, so MSAL-owned message and response-body fields are redacted before the exception is exposed.
- **Preserve downstream control-flow signals.** Redaction must not remove the diagnostic routing tokens that downstream libraries branch on.
- **Keep the public contract small.** The public extensibility surface is the scrubber hook; built-in baseline rules are implementation details.

## 5. Public API design

Two public surfaces: a scrubber delegate and a builder hook.

**Delegate (the extension contract):**
```csharp
public delegate string MsalExceptionScrubber(string content);
```
- Input: a piece of MSAL-owned exception content (a message or a response body).
- Output: redacted content, or the original content if no redaction is needed.
- Contract: may be called multiple times; should be thread-safe, deterministic, idempotent; must not assume a format (plain text vs JSON); should not throw — if it throws, MSAL treats that as scrubber failure and continues with the baseline; **should preserve diagnostic routing tokens (see Section 12).**

**Builder hook:**
```csharp
WithExceptionScrubber(MsalExceptionScrubber scrubber) -> T
```
Placement on the shared root so it is inherited by every application builder:
```
BaseAbstractApplicationBuilder<T>          ← hook lives here
├── ManagedIdentityApplicationBuilder       (derives directly)
└── AbstractApplicationBuilder<T>
    ├── PublicClientApplicationBuilder
    └── ConfidentialClientApplicationBuilder
```
`ManagedIdentityApplicationBuilder` derives directly from the root, so the hook must be on `BaseAbstractApplicationBuilder<T>` to reach public-client, confidential-client, and managed-identity applications. Returns `T` for fluent chaining.

Semantics:
- Null argument throws `ArgumentNullException`.
- Repeated registrations compose (additive); they do not replace.
- The hook adds scrubbers; it does not disable the baseline.

**Delegate shape tradeoff.** The delegate intentionally receives only a string, not a field/source context. If reviewers require field-aware redaction, the API shape must be revisited before shipping.

## 6. Built-in baseline behavior
MSAL may include built-in baseline redaction rules for known classes of sensitive token-like content. These rules are implementation details of the baseline scrubber and are not the public extensibility contract. The public extensibility contract is the host-provided `MsalExceptionScrubber` delegate and builder registration hook. Hosts that need additional protection can register their own content-based scrubber without MSAL code changes.

The built-in baseline:
- is always on and applied to in-scope MSAL-owned content;
- runs after any host scrubbers;
- cannot be disabled through the public hook;
- **excludes protocol error/sub-error codes from its rules (see Section 12)**;
- can evolve internally without changing the public mechanism or contract.

## 7. Redaction pipeline / conceptual architecture
A single pipeline applies, in order:
```
service-derived exception content
    ──► [ host scrubber(s), optional/additive ]
    ──► [ built-in baseline, always on ]
    ──► redacted content
```
1. **Host scrubber(s)** — optional; composed if more than one was registered.
2. **Built-in baseline** — always on; applies built-in baseline redaction rules; cannot be disabled by host scrubbers.

- The baseline runs **last**, so it covers anything a host scrubber leaves behind.
- In-scope MSAL-owned content assignments made while MSAL constructs service-derived exceptions are routed through this internal pipeline.
- **Idempotency requirement:** the full pipeline must be idempotent. Baseline replacement text must not match baseline rules or common redaction placeholders.

## 8. Configuration propagation and context-free paths
The host scrubber is registered on application configuration. Custom host scrubbing is therefore available only on paths where MSAL has access to that configuration or can pass a scrubber from the caller.

- **Context-aware service response paths** use the configured scrubber from the request/application context and pass it into the internal pipeline.
- **Context-free paths** receive the built-in baseline by default. Where genuinely no app configuration exists, the design guarantee is baseline-only.

**Exception-object positioning:**
- Host scrubber delegates are **not** stored on exception objects.
- The public `ResponseBody` setter on `MsalServiceException` is **not** the custom-scrubber funnel. Field-level custom redaction happens before MSAL constructs or assigns MSAL-owned service-derived content.

## 9. Funnel coverage reality
The internal redaction pipeline is centered on MSAL-owned service-derived content assignments made while MSAL constructs exceptions (primarily in `MsalServiceExceptionFactory`).

- **`FromHttpResponse`** can receive request/application context: host scrubber + baseline when present, baseline-only when context is null.
- **`FromBrokerResponse`**, **`FromImdsResponse`**, **`CreateManagedIdentityException`**, **`FromThrottledAuthenticationResponse`** are context-free today and receive baseline-only protection unless their caller plumbing is extended.
- **`SetHttpExceptionData`** is the single factory write point for `ResponseBody`, so response-body redaction has a clear insertion point.
- **Message construction** is broader than `ResponseBody` assignment; message redaction must be applied wherever MSAL constructs or assigns in-scope diagnostic message content (including managed-identity helper paths that place parsed server error text into the message).

Broker integration content is emitted from the broker assembly (`Microsoft.Identity.Client.Broker`), so the built-in baseline scrubber must be reachable across assemblies. This is an internal visibility/packaging detail, not a public API commitment.

Resulting guarantees:
- **Host scrubber + baseline** with app/request configuration.
- **Baseline-only** without app/request configuration.
- **Public setters, direct caller construction, unit-test setup, and JSON rehydration** remain outside the host-configured scrubber guarantee.
- **String rendering** receives a documented baseline-only final pass applied once at the most-derived `ToString()` result.

## 10. Coverage and exclusions
**MSAL-owned message and response-body content on service-derived exceptions is redacted at creation where MSAL owns the content assignment.** Other renderings are redacted only to the extent they derive from those redacted fields — the design does **not** claim blanket redaction.

- **Message / error description** — redacted at creation.
- **Response body / response content** — redacted at creation (single write point at `SetHttpExceptionData`).
- **String rendering (baseline-only final pass):** runs once on the already-rendered outermost `ToString()` result; does not invoke host scrubbers, does not recurse into exception objects; best-effort baseline-only over the rendered string including rendered inner-exception text; expected O(n) with no allocation when nothing matches.
- **JSON / serialized rendering:** message and response body are redacted because those stored fields were redacted before storage. Deserialization/rehydration is not a host-configured redaction path.
- **Paths without application/request configuration** still receive at least the built-in baseline.

**Exclusions / preserved content:**
- **Claims challenges / recovery data** — preserved, never redacted. Serialized output is not claimed fully sanitized with respect to claims.
- **Inner / nested exceptions** — at most best-effort baseline protection during string rendering.
- **`AdditionalExceptionData`** — in scope when MSAL emits string diagnostic content: host + baseline where app/request configuration is available, otherwise baseline-only; recovery data excluded. Managed-identity and broker paths that populate it are context-free today (baseline-only unless re-plumbed).

## 11. Failure behavior and safety
- **Host scrubber invocation is isolated.** If a host scrubber throws — including throwing an MSAL exception — the pipeline treats it as scrubber failure and continues with the original content plus baseline. Scrubber failure must not recursively trigger additional custom scrubbing or change the exception type/control flow.
- **Null / empty result, with empty-message invariant:**
  - Host scrubber returns null → treated as no change.
  - **Hard invariant:** required exception messages must never become null, empty, or whitespace after redaction because the base MSAL exception constructor rejects null/whitespace messages. If redaction produces an empty/whitespace message, MSAL falls back to a generic non-empty message.
  - **For optional response-body content, empty output may be preserved as intentional redaction, but redaction must not convert a non-null `ResponseBody` into null.** Empty is allowed for intentional redaction; null is not introduced by redaction. Some downstream libraries inspect `ResponseBody` for diagnostic routing; non-null input must produce non-null output.
- **Redaction must preserve diagnostic routing tokens such as `AADSTS*` and `AADB2C*` error-code strings.** These codes are not secrets; they are control-flow signals used by downstream libraries (see Section 12).
- Redaction never changes exception type, error code, status code, correlation ID, retryability, or control flow — only human-readable/raw content strings.
- The baseline cannot be disabled via the host hook.
- Negligible cost when nothing matches.

## 12. Downstream diagnostic-routing compatibility
Some downstream libraries intentionally inspect MSAL exception content for diagnostic routing, not just logging. Redaction must preserve those routing signals.

Microsoft.Identity.Web has two distinct exception-content routing dependencies, on different exception types and different fields:
- **Certificate-retry routing** inspects `MsalServiceException.ResponseBody` after first gating on `MsalServiceException.ErrorCode == invalid_client`. It scans `ResponseBody` for STS certificate/signed-assertion failure codes.
- **B2C/password-reset routing** inspects `MsalUiRequiredException.Message` for `ErrorCodes.B2CPasswordResetErrorCode`.

If `MsalUiRequiredException.Message` is built through an in-scope MSAL redaction path, preservation of the B2C diagnostic code in `Message` is an active redaction guarantee. If that `Message` is built on a context-free or non-funneled path, it is covered only by the baseline-exclusion guarantee: the built-in baseline must never target `AADSTS*`/`AADB2C*` protocol codes. In both cases, the practical compatibility requirement is the same: the B2C diagnostic code (`ErrorCodes.B2CPasswordResetErrorCode`) must remain detectable by ID.Web.

Therefore, routing-token preservation is not limited to `MsalServiceException`. The invariant is field/content based: built-in baseline redaction rules must exclude `AADSTS*`/`AADB2C*` protocol error-code tokens, and MSAL-owned redaction must be surgical wherever MSAL redacts in-scope `Message` or `ResponseBody` content at creation.

Compatibility invariants:
- Redaction must not change `MsalServiceException.ErrorCode`.
- MSAL-owned redaction and the built-in baseline must not remove or rewrite diagnostic routing tokens such as `AADSTS*` and `AADB2C*` error-code strings from **any in-scope redacted `Message` or `ResponseBody` field, regardless of whether the carrying exception type is `MsalServiceException`, `MsalUiRequiredException`, or another service-derived MSAL exception type.**
- Built-in baseline redaction rules must **exclude** protocol error/sub-error codes. These are diagnostic routing signals, not secrets.
- Redaction must not convert a non-null `ResponseBody` into null. If response-body content is redacted entirely, the result must be an empty string or a redaction placeholder, not null.
- Redaction must be **surgical**: replace only matched sensitive substrings, not the entire `Message` or `ResponseBody`, so diagnostic routing codes elsewhere in the same content survive.
- Arbitrary host scrubbers are opaque. The public contract documents that they should preserve diagnostic routing tokens unless MSAL later adds a preservation post-pass.

**Enforcement tiers (v1):**
- **MSAL-owned redaction and the built-in baseline:** *must preserve* routing tokens (guaranteed by MSAL).
- **Microsoft-provided host scrubbers (including ID.Web's marker-based scrubber):** *must enforce* the same rule.
- **Arbitrary host scrubbers:** opaque; the public contract *documents* the preservation expectation. This becomes an MSAL guarantee only if MSAL later adds a preservation post-pass over routing tokens.

*(The concrete value behind `ErrorCodes.B2CPasswordResetErrorCode` — for example, the current value in the ID.Web constants file — is intentionally not hardcoded here; the load-bearing contract is preserving the symbolic B2C diagnostic routing code that ID.Web scans for.)*

## 13. Compatibility and behavior change
- **Additive, net-new public surface:** one delegate type (`MsalExceptionScrubber`) and one builder method (`WithExceptionScrubber`). No breaking changes to existing APIs, no exception-type or error-code changes, no control-flow changes.
- **Behavior change to call out:** with redaction at creation, `Message` and `ResponseBody` may now return redacted text where they previously returned raw content; additionally, the baseline-only `ToString` final pass changes string rendering. Document in release notes.
- **Public API review** must include `PublicAPI.Unshipped.txt` updates for the `MsalExceptionScrubber` delegate and the `WithExceptionScrubber` method. The exact entries are determined by the repo tooling. Whether any baseline redaction patterns are exposed as public API is a separate decision (see Open questions); otherwise the baseline rules remain implementation details.

## 14. Microsoft.Identity.Web integration
Microsoft.Identity.Web can opt into the MSAL exception scrubber hook by exposing a nested exception-handling options object under the existing Microsoft identity application options. This allows applications using ID.Web to configure additional exception-content redaction through appsettings or programmatic options.

**Example configuration:**
```json
{
  "AzureAd": {
    "ExceptionHandling": {
      "ScrubbingMarkers": [ "marker-a", "marker-b" ],
      "RedactionPlaceholder": "[REDACTED]",
      "MarkerMatchIsCaseSensitive": false
    }
  }
}
```
The options are grouped under `ExceptionHandling` rather than added as multiple top-level `AzureAd` properties, leaving room for future exception-handling or diagnostic-security knobs without expanding the top-level configuration surface.

**Conceptual public surface** (aligned to the sample above):
- `MicrosoftIdentityExceptionHandlingOptions`
  - `ScrubbingMarkers`
  - `RedactionPlaceholder`
  - `MarkerMatchIsCaseSensitive`
- `MicrosoftIdentityApplicationOptions.ExceptionHandling`

*(If the v1 options type ships only `ScrubbingMarkers`, remove `RedactionPlaceholder` and `MarkerMatchIsCaseSensitive` from the sample and list them as future extensions; the sample and the type must stay aligned.)*

**ID.Web wiring:** ID.Web binds the `ExceptionHandling` subsection through the existing options binding path, carries it through merged options, and registers `WithExceptionScrubber` at the MSAL builder creation point when configured markers exist.
- For confidential-client based flows, the existing confidential-client application build path is the natural registration point.
- Managed identity is a separate application path and is **not** covered by the confidential-client build. It must be wired separately if ID.Web host scrubbing is required there; otherwise it remains baseline-only through MSAL's built-in baseline.

**Compatibility invariant:** ID.Web uses MSAL exception content for control flow in two distinct ways:
- Certificate retry depends on `MsalServiceException.ErrorCode == invalid_client` and `MsalServiceException.ResponseBody` containing STS certificate/signed-assertion failure codes.
- B2C/password-reset routing depends on `MsalUiRequiredException.Message` containing `ErrorCodes.B2CPasswordResetErrorCode`.

Therefore, any ID.Web-provided scrubber must preserve `AADSTS*`/`AADB2C*` diagnostic routing codes across any MSAL exception `Message` or `ResponseBody` content it redacts, and must not convert a non-null `ResponseBody` to null.

**ID.Web scrubber behavior:**
- Redacts only app-configured marker strings.
- Uses surgical substring replacement, not whole-body replacement.
- Filters or ignores configured markers that look like protocol diagnostic routing codes (`AADSTS*`, `AADB2C*`).
- Does not convert non-null content to null.
- Composes with MSAL's built-in baseline through `WithExceptionScrubber`; does not replace or disable the baseline.

**Public API note:** public API review is required for the new options type and its public members, plus the `ExceptionHandling` property on `MicrosoftIdentityApplicationOptions`. The exact `PublicAPI.Unshipped.txt` entries are determined by the repo tooling (which may list the type, constructor, and individual getters/setters separately). ID.Web ships no marker values; applications supply their own.

## 15. Decisions made
- The generic public mechanism is a scrubber delegate plus a builder registration hook, independent of the built-in baseline rules.
- The hook lives on `BaseAbstractApplicationBuilder<T>` so it is available to public-client, confidential-client, and managed-identity applications.
- Host scrubbers are additive; repeated registrations compose; the baseline runs last and is not disabled by the public hook.
- MSAL-owned message and response-body content on service-derived exceptions is redacted at creation; a baseline-only `ToString` final pass runs once at the outermost rendering.
- Custom host scrubbing applies where app/request configuration is available; otherwise baseline-only. Managed-identity and broker paths are context-free today (baseline-only unless re-plumbed).
- Claims challenges / recovery data are preserved and not redacted.
- **Routing-token preservation is field/content based, not limited to `MsalServiceException`.** MSAL-owned redaction and built-in baseline rules preserve `AADSTS*`/`AADB2C*` diagnostic routing codes in any in-scope redacted `Message` or `ResponseBody` field, including `MsalServiceException.ResponseBody` for certificate retry and `MsalUiRequiredException.Message` for B2C/password-reset routing, to the extent those fields are constructed on an in-scope redaction path. Where a field is not funneled, the built-in baseline still excludes those protocol codes from its rules.
- `ResponseBody` redaction does not convert non-null content to null. Redaction is surgical and replaces only matched sensitive substrings.
- Routing-token preservation is guaranteed for MSAL-owned/baseline redaction and must be enforced by Microsoft-provided host scrubbers (including ID.Web's). Arbitrary host scrubbers carry a documented contract to preserve routing tokens unless MSAL adds a preservation post-pass.
- Built-in baseline redaction rules are implementation details and not part of the public extensibility contract; they exclude protocol error/sub-error codes.
- Redaction does not change exception type, error code, status code, correlation ID, retryability, or control flow.

## 16. Open questions
- Should any baseline redaction patterns be exposed as public API, or remain implementation details of the built-in baseline scrubber?
- Should the public scrubber remain string-only, or include field/source context?
- Should MSAL add an explicit routing-token preservation post-pass so the guarantee extends to arbitrary host scrubbers, not just Microsoft-provided ones?
- **Does `MsalUiRequiredException.Message` flow through the same in-scope redaction path as `MsalServiceException` service-response content? If yes, B2C diagnostic-code preservation in `Message` is an active redaction guarantee. If no, it is covered only by the baseline-exclusion guarantee. The validation matrix must match the answer.**
- Should context-free integration paths (managed identity, broker) be re-plumbed to carry app/request configuration in v1, or is baseline-only acceptable?
- What redaction placeholder format should the built-in baseline use?
- Post-v1: should serialized output offer a diagnostic-safe mode that also redacts preserved recovery fields such as claims?

## 17. Validation matrix
**Core mechanism**
- `FromHttpResponse` with app/request context: host scrubber + baseline; with null context: baseline-only.
- Invalid-grant / UI-required, claims-challenge (Claims preserved, message redacted), invalid-client (OAuth error description), authority/token-endpoint augmentation.
- `FromBrokerResponse`, `FromImdsResponse`, `CreateManagedIdentityException`, `FromThrottledAuthenticationResponse`: baseline-only unless re-plumbed (including managed-identity paths where raw response body or parsed server error text enters `Message`).
- `SetHttpExceptionData` response-body assignment; `AdditionalExceptionData` (host+baseline where config exists, baseline-only otherwise; broker-assembly emit reachable cross-assembly).
- Throwing host scrubber → original + baseline; null host scrubber result → no change + baseline; empty required message → generic non-empty fallback.
- Double-run idempotency; adversarial host/baseline idempotency.
- `ToString` final pass applied once at outermost rendering (no double-apply); inner-exception text best-effort baseline-only.
- `ToJsonString` uses already-redacted stored fields; `FromJsonString`/rehydration not claimed as host-configured redaction.

**Downstream diagnostic-routing compatibility**
- Certificate-retry compatibility: an `invalid_client` `MsalServiceException` whose `ResponseBody` contains a certificate/signed-assertion `AADSTS` code still exposes that code after redaction.
- `ResponseBody` null compatibility: redaction does not convert a non-null `ResponseBody` into null.
- Diagnostic routing token preservation: `AADSTS*` and `AADB2C*` error-code tokens are preserved in in-scope redacted `Message` and `ResponseBody` fields while adjacent sensitive token-like material is redacted.
- **B2C/password-reset message-routing compatibility:** a `MsalUiRequiredException` whose `Message` contains `ErrorCodes.B2CPasswordResetErrorCode` still exposes that code after redaction, if that `Message` is on an in-scope redaction path; otherwise validate that the built-in baseline does not target that code.
- Surgical redaction: a body containing both sensitive token-like content and a diagnostic routing code redacts only the sensitive substring and leaves the routing code intact.

**Microsoft.Identity.Web integration**
- Configuration binding populates `ExceptionHandling.ScrubbingMarkers` (and `RedactionPlaceholder` / `MarkerMatchIsCaseSensitive` if present).
- Null `ExceptionHandling` ⇒ no ID.Web host scrubber registered; MSAL baseline still applies.
- Empty/whitespace markers ignored; markers resembling `AADSTS*`/`AADB2C*` routing codes ignored and do not break ID.Web routing.
- Configured secret marker redacted from MSAL exception `Message` / `ResponseBody`.
- Certificate-retry compatibility: `invalid_client` `MsalServiceException` with `ResponseBody` containing a certificate/signed-assertion `AADSTS` code still exposes that code after ID.Web redaction.
- B2C/password-reset compatibility: `MsalUiRequiredException` whose `Message` contains `ErrorCodes.B2CPasswordResetErrorCode` still exposes that code after ID.Web redaction.
- Non-null `ResponseBody` remains non-null after ID.Web redaction.
- Confidential-client based flows receive the configured ID.Web scrubber.

