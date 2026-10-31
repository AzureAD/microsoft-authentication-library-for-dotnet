# User token cache read isolation

Synchronized confidential-client user caches hydrate an operation-owned cache
directly by default when legacy ADAL compatibility is disabled, an in-memory
accessor is used, and the request has a known user partition. Different requests
can hydrate their partitions concurrently without replacing each other's decoded
token and account state. MSAL does not first hydrate the shared cache and then
copy its entire contents.

Persisted keys and MSALv3 bytes do not change. The private cache is decoded
working state for one operation, not a second persistent token store. Its state
is reused by that operation's token/account lookups and is eligible for collection
when the operation no longer references it.
Successful private reads do not populate the original cache. Serializing that
original cache outside notifications is not a snapshot of the last hydrated
partition; serializers must use the notification target.

The full `BeforeAccess` and `AfterAccess` read lifecycle, including external L1/L2
publication, is coordinated with exclusive writes and removals on the original
cache. Public clients, application-token caches, native-platform accessors,
disabled internal caches, legacy-enabled caches, whole-cache/no-key operations,
and caches with synchronization disabled retain their existing behavior.

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
  `DeserializeMsalV3(payload, shouldClearExistingCache: true)` or
  `ITokenCacheSnapshotProvider.RestoreSnapshot`. A null payload can represent a
  completely empty partition.
- Callbacks use `args.TokenCache`, not a captured original cache. They tolerate
  overlapping read lifecycles, including any external L1/L2 publication and their
  own mutable state.
- All cache work is awaited before the callback completes. Background publication
  after a callback returns is outside the coordinated lifecycle.

Read callbacks receive an operation-owned cache; write callbacks still receive
the original cache. Captured-root, incremental, load-once, and read callbacks that
do not hydrate the supplied cache are not compatible. Contract violations throw
`NotSupportedException` with opt-out guidance rather than silently continuing with
an empty cache. Providers must not use notification-cache object identity as a
stable key for their external storage or shared synchronization.

Isolation is local to one cache instance. It does not add distributed transactions,
refresh single-flight, cross-instance logout coordination, or guarantees about
late token-endpoint responses. Private hydration still allocates decoded cache
state; concurrency is not a promise that every warm single-threaded lookup is
cheaper. Qualify custom providers and representative workloads before rollout.

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
