# MSAL.NET Regression Patterns

These examples guide investigation. They are not automatic findings.

## Validation Before Canonicalization

A raw authority is rejected before known-cloud metadata maps an alias to its preferred host.

Investigate when:

- Validation reads original input while downstream code uses a normalized value.
- A positive alias test is removed and the same alias is added to a negative test.
- Only a canonical representation remains covered.

Check authoritative public metadata, equivalent aliases, regional behavior, and global behavior.

## Public Result Semantics Change

A public result field changes format or source without a public signature change.

Check:

- Equivalent tenant and authority forms.
- Network and cache-result paths.
- Canonical service data versus caller input.
- Public downstream consumers that parse or forward the value.

## Exception Boundary Changes

An operation moves behind another client or abstraction while catch filters remain tied to the old exception shape.

Check service errors, raw transport or TLS failures, cancellation, retry counts, recovery, and final propagation.

## Cache Identity Omits a Dimension

Two non-interchangeable tokens or credentials share cache identity.

Check tenant, account, authority, claims, attributes, certificate or key identity, token type, region, and other request dimensions. Test same-state hits and changed-state misses.

## Async Lifetime Becomes Shorter

A cancellation source, stream, request, handler, or certificate is disposed before returned asynchronous work completes.

Check cancellation while queued and during I/O, release on exception paths, and unbounded background work.

## Custom Transport Is Bypassed

A builder accepts a supported custom factory, but a special host or platform path constructs an internal client.

Check factory invocation counts, request routing, proxies, certificates, redirect policy, and whether the behavior is new.

## Platform or Serializer Gap

A desktop path is updated while mobile, broker, trimming, serialization, or NativeAOT metadata is incomplete.

Build or test the affected target. Verify source-generation roots, polymorphic types, trimming annotations, native assets, and runtime identifiers.

## Public Warning Breaks Downstream Builds

An obsolete, analyzer, nullable, or visibility annotation is described as nonbreaking because local source still compiles.

Check warning-as-error consumers and require a staged migration when public downstream code uses the member.

## Redirect or Retry Replays Credentials

Automatic or manual redirect behavior forwards a credential, assertion, token, certificate, or request body to another origin.

Check scheme, normalized host, effective port, redirect count, total timeout, retry budget, and whether custom transports can enforce the same policy.

## Protocol Correlation or Proof Binding Is Lost

State, nonce, PKCE, issuer, audience, assertion, certificate, or proof-binding validation is removed, reordered, cached under incomplete identity, or enforced on only one transport or platform.

Check generation, persistence, comparison, expiry, replay, refresh, cache-hit, broker, and custom-transport paths. A successful token response does not prove the request and response remain bound to the initiating client, user, tenant, or key.

## Header Lookup Becomes Case-Sensitive

An HTTP abstraction compares protocol header names with ordinal case-sensitive logic or preserves only one casing variant.

Check response parsing, authentication challenges, correlation headers, retry metadata, custom transports, and platform handlers with equivalent header casing.

## Configuration or Kill Switch Is Frozen

Mutable configuration, environment state, feature flags, cloud metadata, or a kill switch is read once into static state when callers expect later changes to take effect.

Check process lifetime, test ordering, tenant or application isolation, refresh behavior, rollback safety, and whether the contract requires snapshot or dynamic semantics.

## Certificate or Key Lifecycle Changes

A cryptographic path changes certificate selection, algorithm support, key usage, exportability assumptions, rotation, expiry, disposal, or proof binding.

Check both acquisition and cache-hit paths, rollover overlap, hardware-backed and non-exportable keys, platform differences, assertion validation, and fail-closed behavior.

## Optional Feature Bloats General Packages

A feature-specific dependency or native asset appears in ordinary package or publish output.

Inspect packed contents and dependency graphs for each affected target framework and runtime identifier.

## Test Inversion Is Used as Proof

A positive scenario becomes an expected failure in the same change that adds the rejecting guard.

The new test proves only the new behavior. Find independent contract evidence before classifying the prior behavior as unsupported.

## Telemetry Alters Functional State

Caller or SDK telemetry starts participating in cache, routing, query, retry, or token identity.

Verify requests that differ only in telemetry remain functionally equivalent unless the change is explicitly designed and documented.
