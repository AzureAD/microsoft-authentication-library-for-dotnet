# MSAL.NET Review Lenses

Apply only the lenses affected by the pull request.

## Functional Correctness

- Trace changed branches, guards, early returns, and state transitions.
- Check null, empty, malformed, duplicate, stale, expired, and boundary inputs.
- Verify that the implementation covers the linked issue rather than only its simplest example.

## Behavioral Compatibility

- Identify newly accepted, rejected, rerouted, retried, cached, or exposed scenarios.
- Check defaults, normalization order, output formats, exception behavior, and side effects.
- Require impact and migration information for intentional breaking changes.

## Public API and Extensibility

- Verify public API baselines and XML documentation.
- Check source, binary, behavioral, analyzer, nullable, and warning compatibility.
- Ensure builders, callbacks, factories, delegates, and supported extension points are honored.
- Check public downstream use before changing or obsoleting a member.

## Authentication, Protocol, Authority, and Endpoint Behavior

- Verify authority parsing, aliases, canonicalization, cloud metadata, tenant forms, regionalization, and endpoint construction.
- Preserve OAuth and OpenID Connect invariants for state, nonce, PKCE challenges and verifiers, issuer, audience, scopes, resources, assertions, token types, and authentication schemes.
- Verify assertion audience, subject, expiry, replay resistance, and proof or certificate binding across acquisition, cache, refresh, and redemption paths.
- Ensure validation occurs after required normalization and discovery.
- Check public, sovereign, private, and custom-authority paths when the same rule applies.

## Cache, Persistence, Identity, and State

- Include every non-interchangeable token or credential property in cache identity and filtering.
- Check cold acquisition, cache hits, force refresh, removal, expiry, restart, and rotation.
- Check configuration and kill-switch lifetime, refresh behavior, process-wide static caches, and test isolation.
- Do not freeze mutable configuration, environment, cloud metadata, or feature flags into static state unless the contract explicitly requires a process-lifetime snapshot.
- Prevent state crossing tenants, accounts, authorities, certificates, token types, platforms, or requests.

## Async, Concurrency, Lifetime, and Cancellation

- Check races, synchronization, locks, semaphores, queues, and mutable static state.
- Verify cancellation reaches waits, retries, callbacks, background work, and HTTP calls.
- Keep disposables alive until asynchronous work completes.
- Verify exactly-once behavior for callbacks, factories, refreshes, retries, and cleanup.

## Errors, Retry, Fallback, Timeout, and Recovery

- Compare exception types and inner-exception chains before and after delegation.
- Separate service errors, transport failures, TLS failures, cancellation, and programming errors.
- Check total retry and timeout budgets, idempotency, and final propagation.
- Ensure fallback does not hide security or service failures or silently change token semantics.

## HTTP, Redirects, Proxies, Certificates, and Custom Transports

- Ensure supported caller-provided factories and handlers are not bypassed.
- Check redirect limits, origin validation, request replay, proxy behavior, certificate validation, and fail-closed behavior on every transport and platform path.
- Treat HTTP header names as case-insensitive. Verify duplicate, joined, stripped, and forwarded header behavior at abstraction boundaries.
- Prevent credentials, assertions, tokens, certificates, and request bodies from reaching untrusted origins.
- Verify certificate and key selection, identity, algorithm, key usage, exportability assumptions, rotation, expiry, disposal, and mTLS or proof binding.

## Platform, Target Framework, Broker, Trimming, Serialization, and NativeAOT

- Check every affected target framework and conditional-compilation path.
- Do not infer mobile or broker safety from desktop tests.
- Verify serializer roots, polymorphic types, trimming annotations, reflection requirements, native assets, and runtime identifiers.
- Check shared public contracts for platform parity.

## Security, Privacy, Telemetry, and Logging

- Check token, secret, assertion, certificate, account, tenant, and PII handling.
- Prevent sensitive data from entering logs, exceptions, serialization, or telemetry.
- Check authorization boundaries, tenant isolation, issuer and audience validation, replay resistance, redirect validation, and unsafe fallback.
- Require fail-closed behavior when custom transports, platform adapters, feature flags, discovery, or cryptographic validation cannot enforce a security invariant.
- Ensure telemetry-only data does not alter cache identity or functional behavior unless intentional.

## Build, Packaging, Dependencies, and Assets

- Check transitive dependencies, package contents, binding behavior, native files, PDBs, and package size.
- Verify target framework, SDK, runtime, warning-as-error, and publish behavior.
- Keep optional feature dependencies out of packages used by unrelated consumers.
- Reject generated logs, build output, or machine-specific files.

## Performance and Resource Management

- Check eager allocation, repeated discovery, excess network calls, cache fragmentation, contention, and unbounded growth.
- Check HTTP clients, handlers, sockets, streams, certificates, and native resource lifetimes.
- Require invocation counts for pooling, lazy creation, retry, and cache fixes.

## Test Adequacy and Integrity

- Treat removed, narrowed, and inverted positive tests as compatibility evidence.
- Do not accept tests that merely encode the new implementation.
- Test observable side effects, not only final success.
- Cover every changed decision boundary and each materially equivalent representation.
