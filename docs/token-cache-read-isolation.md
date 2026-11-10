# User token cache read isolation

Synchronized confidential-client user caches hydrate an operation-owned cache
directly by default when legacy ADAL compatibility is disabled, an in-memory
accessor is used, and the request has a known user partition. Async-only providers
can hydrate different requests concurrently without replacing each other's decoded
token and account state. MSAL does not first hydrate the shared cache and then
copy its entire contents.

Persisted keys and MSALv3 bytes do not change. The private cache is decoded
working state for one operation, not a second persistent token store. Its state
is reused by that operation's token/account lookups and is eligible for collection
when the operation no longer references it.
MSAL publishes successful private reads into
the existing original accessor only after both awaited callbacks succeed.
Publication clones mutable cache-item wrappers and nested state once, shares
immutable credential strings, and deep-copies unknown JSON without another JSON
encode/decode or snapshot capture/restore. Private lookup ownership is retained.
MSALv3 `clear=true` and structured restores replace prior root entries;
`clear=false` merges warm partitions, preserving discovery ambiguity. Unknown JSON
follows the last nonempty deserialize/structured restore; explicit empty replacement
clears it. A short root publication/serializer gate coordinates completed copies,
not Redis/provider I/O. Writers and removals remain excluded by the lifecycle lease.

The full `BeforeAccess` and `AfterAccess` read lifecycle, including external L1/L2
publication, is coordinated with exclusive writes and removals on the original
cache. When either read notification has a synchronous callback, the entire
lifecycle remains sequential. Async-only read lifecycles can overlap by default.
This preserves synchronous file-provider coordination without serializing
async-only distributed-cache reads. Async registration alone does not make a
provider's shared state thread safe.
Public clients, application-token caches, native-platform accessors,
disabled internal caches, legacy-enabled caches,
and caches with synchronization disabled retain their existing behavior.
Whole-cache/no-key operations retain original-root callbacks under their exclusive
lifecycle and then pin an independently owned decoded copy before releasing the
root. Later keyed publication cannot change that session's lookups, including
when an account is subsequently resolved from a login hint.

This changes the serialization contract for qualifying operations. Existing
serializers must meet the requirements below or use the opt-out. MSAL does not
silently retry incompatible callbacks against the original cache.

## Rollback

Set this process-wide switch before constructing the MSAL application:

```csharp
AppContext.SetSwitch("Microsoft.Identity.Client.DisableCacheReadIsolation", true);
```

The choice is captured when the cache is constructed. Changing the switch later
does not reconfigure existing applications. Rollback restores the previous shared
read behavior, including the possibility of another hydration replacing a
request's decoded cache state.

## Serializer requirements

For qualifying operations:

- Every `BeforeAccess` callback completely hydrates `args.TokenCache`, using
  `DeserializeMsalV3(payload)` (with either clear-existing flag) or
  `ITokenCacheSnapshotProvider.RestoreSnapshot`. A null payload can represent a
  completely empty partition. MSAL clears the private target, including unknown
  JSON, before each hydration attempt. A failed attempt cannot supply the base
  state for a later retry.
- Callbacks use `args.TokenCache`, not a captured original cache. Async-only
  providers tolerate overlapping read lifecycles, including any external L1/L2
  publication and their own mutable state. Providers that require sequential
  async lifecycles must use the opt-out.
- All cache work is awaited before the callback completes. Background publication
  after a callback returns is outside the coordinated lifecycle.

Awaited hydration may run with execution-context flow suppressed. Complete-hydration
validation follows the supplied private target, not the callback's execution context.

Known-partition read callbacks receive an operation-owned cache; write callbacks still receive
the original cache. A complete snapshot using the default MSALv3 overload is
supported; genuinely incremental providers that depend on prior in-memory state
are not. Captured-root, load-once, and read callbacks that
do not hydrate the supplied cache are not compatible. Contract violations throw
`NotSupportedException` with opt-out guidance rather than silently continuing with
an empty cache. Providers must not use notification-cache object identity as a
stable key for their external storage or shared synchronization.

Successful keyed hydration warms the original cache for subsequent independent
account/login-hint discovery, using the existing eligibility algorithm (including
refresh-token, client and environment filtering). Explicit unkeyed restores take
precedence over warm state. Failed private callback lifecycles never publish.
No separate discovery cache or new persistent format is introduced.

Request coalescing renews an isolated session's read view after acquiring admission,
so queued callers can observe the preceding acquisition's completed publication.
Renewal uses the same hydration, lifecycle coordination and publication rules; it
does not read tokens directly from the shared root. Subsequent token, ID-token and
account lookups stay pinned to the renewed view. Ordinary warm hits do not renew.

Isolation itself is local to one cache instance. It does not add distributed transactions,
refresh single-flight, cross-instance logout coordination, or guarantees about
late token-endpoint responses. Private hydration still allocates decoded cache
state; concurrency is not a promise that every warm single-threaded lookup is
cheaper. Qualify custom providers and representative workloads before rollout.
Successfully published state remains in the original cache until replacement,
removal or the cache's lifetime ends. Providers using `clear=false` can retain
multiple observed user partitions, so include original-cache retention in memory
and GC measurements.

## Rollout

Qualify the configured serializer, not only the MSAL application registration.
Exercise cold and warm cache hits, persistence, refresh, account removal,
cancellation and provider failures with the actual L1/L2 and encryption settings.
Canary the change and compare cache-hit failures, token-endpoint traffic, callback
latency, allocation/GC, L2 concurrency and write/removal latency. A higher number
of overlapping reads can increase backend load even when request latency improves.

If compatibility or capacity regresses, enable the rollback switch and recreate
the application, normally by restarting the process. Do not retry a partially
executed callback automatically against the shared cache.
