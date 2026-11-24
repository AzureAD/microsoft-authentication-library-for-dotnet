# MSAL.NET Token Cache Architecture: Bounded L1 and Extensible Persistence

**Status:** Working design proposal
**Audience:** MSAL.NET, Seal, Microsoft.Identity.Web, and MISE
**Scope:** Application (S2S) and user (C2S/OBO) token caching

## Executive summary

MSAL.NET already keeps tokens in an internal in-memory cache by default. The
problem to solve is not merely how to turn caching on: the current in-memory
accessors are unbounded dictionaries, while external persistence and L1
partitioning are coordinated through callbacks with provider-owned
synchronization. The Seal team has reported memory-growth and L1/L2
consistency problems. These reports motivate the design; each suspected root
cause must still be reproduced and measured before being called a confirmed
leak or defect.

The recommended direction is:

1. MSAL owns token identity, cache lookup, synchronization, memory accounting,
   LRU recency, and eviction. Store each access token as an independently
   addressable entry rather than relying on one opaque application partition
   containing tokens for unrelated resources.
2. Keep the L1 policy independent from persistence. A future narrow,
   optional persistence provider stores opaque, versioned MSAL records. Do not
   expose `ITokenCacheInternal`, cache-item classes, or mutable dictionaries as
   public extension points.
3. Keep the existing `ITokenCache` notifications, V3 serialization format,
   and suggested-cache-key behavior compatible. Introduce the managed LRU
   behind an opt-in/gradual rollout before considering a safe default.
4. Start with application access tokens, with a proposed **10 MiB process-wide
   MSAL-managed application-token L1 budget**. Use the same identity and
   persistence model for user tokens, but ship user-token eviction only after
   its account, refresh-token, and OBO policies are qualified.

The budget is an accounting limit for MSAL-owned cache entries, not a hard
process-memory limit. It does not include application objects, provider
overhead, concurrent requests, or all CLR bookkeeping.

## 1. Problem statement and evidence

### Reported problem

Seal reports memory growth, leaks, and synchronization failures when MSAL L1
state is combined with application-managed L1/L2 stores. The suspected cause
is split ownership: MSAL owns its in-memory dictionaries and lookup behavior,
while callback providers own persistence, partition selection, concurrency,
and often another L1 cache.

This design targets bounded retention and coherent cache operations. It does
not assume every reported memory leak has this single cause. Instrument and
reproduce the reported cases before attributing individual incidents.

### Relevant current behavior

- The default application and user accessors use nested
  `ConcurrentDictionary` instances without a byte limit or LRU eviction.
  `CacheOptions.UseSharedCache` uses static dictionaries. Application tokens
  are partitioned by client and tenant (plus applicable authentication-scheme
  and extension components); the partition can contain tokens for many
  resources. See
  [`InMemoryPartitionedAppTokenCacheAccessor`](../../src/client/Microsoft.Identity.Client/PlatformsCommon/Shared/InMemoryPartitionedAppTokenCacheAccessor.cs),
  [`InMemoryPartitionedUserTokenCacheAccessor`](../../src/client/Microsoft.Identity.Client/PlatformsCommon/Shared/InMemoryPartitionedUserTokenCacheAccessor.cs),
  and [`CacheOptions`](../../src/client/Microsoft.Identity.Client/AppConfig/CacheOptions.cs).
- The callback `suggestedCacheKey` is a *partition hint*, not a unique
  access-token identity. For client credentials it currently derives from
  client ID and input tenant plus applicable components; it does not identify
  one resource token. The internal access-token key separately includes
  environment, tenant, client, scopes, token type, and extension components.
  New persistence must not treat the callback hint as a complete token key.
  See [`CacheKeyFactory`](../../src/client/Microsoft.Identity.Client/Cache/CacheKeyFactory.cs)
  and [`MsalAccessTokenCacheItem`](../../src/client/Microsoft.Identity.Client/Cache/Items/MsalAccessTokenCacheItem.cs).
- Access-token lookup allows a cached token whose returned scope set contains
  the requested scopes. The identity platform can return more scopes than
  requested. Save logic removes overlapping-scope entries in a matching
  context, and lookup fails rather than arbitrarily selecting among multiple
  matches. A new index must preserve these safety properties.
- OBO access and refresh tokens use the incoming assertion hash by default;
  long-running OBO can use an application-supplied key. User access tokens
  otherwise use the home account ID as their cache partition. See
  [`CacheKeyFactory`](../../src/client/Microsoft.Identity.Client/Cache/CacheKeyFactory.cs)
  and [`TokenCache.ITokenCacheInternal`](../../src/client/Microsoft.Identity.Client/TokenCache.ITokenCacheInternal.cs).
- Claims-bearing requests bypass normal access-token lookup in current
  client-credential and OBO paths, with a specific access-token-hash exception
  for client credentials. Claims are not currently a stable token-key
  component. Preserve each flow's behavior; do not store raw claims in keys.
- Refresh tokens and ID tokens are separate credential types with different
  identities and lifetimes. In particular, family refresh tokens are shared
  across applications by design. Application-token caching currently expects
  access tokens, not refresh or ID tokens.
- Cache notifications may serialize a cache partition. MSAL's callbacks,
  internal cache, and external L1/L2 do not by themselves provide a
  cross-process transaction or stale-entry invalidation protocol. The
  existing read-isolation contract and its limitations are documented in
  [`token-cache-read-isolation.md`](../token-cache-read-isolation.md).

## 2. Goals and non-goals

### Goals

- Bound MSAL-managed in-process cache retention and define eviction behavior.
- Remove the need for callers to invent an application-token partitioning and
  synchronization scheme merely to get a useful MSAL L1.
- Keep cache lookup, key derivation, LRU state, and token lifecycle decisions
  inside MSAL.
- Allow persistence in Redis, SQL, Cosmos DB, or another store without
  exposing mutable MSAL internals.
- Support S2S application tokens first and preserve a path to web-app, OBO,
  and public-client user tokens.
- Prevent cross-client, cross-tenant, cross-cloud, cross-user, and
  cross-assertion token confusion.
- Preserve existing public APIs, MSAL V3 interop, unknown cache properties,
  and existing Microsoft.Identity.Web and MISE integrations.
- Make contention, eviction, stale reads, misses, and retained size observable
  without logging tokens, assertions, claims, or PII.

### Non-goals

- Claim that LRU fixes every memory leak, refresh-token race, or provider bug.
- Change OAuth protocol behavior, token lifetimes, or the meaning of existing
  cache callbacks in the first release.
- Replace MSAL V3 as the interoperable serialization format.
- Put client secrets, private keys, raw assertions, access tokens, or claims
  into cache keys or telemetry.
- Guarantee a hard upper bound on process heap size from an estimated entry
  size.
- Make `IDistributedCache` alone provide atomic compare-and-swap,
  cross-process LRU, or invalidation; its basic key/value API does not express
  those guarantees.

## 3. Cache identity model

Use a versioned, structured logical key internally. Its canonical encoding
must be unambiguous (length-prefixed fields or an equivalent structured
encoding), ordinal where identifiers require it, and hashed before use as an
external store key. Keep the structured components available internally for
correct filtering and deletion. Never log the raw key when it contains user
or OBO identifiers.

The token's returned fields and validated request context determine identity;
the HTTP request's unnormalized spelling is not itself a key. The key must
separate credentials with different authorization semantics, while avoiding
secret material as a dimension.

### Access tokens

| Scenario | Proposed identity dimensions | Explicitly not an identity dimension |
|---|---|---|
| S2S / app-only | Credential kind (`AT`), client ID, canonical cloud/environment, specific tenant, normalized returned scope set, token type/authentication scheme and sender-binding key ID when applicable, plus every explicitly cache-affecting request component (for example FMI path, credential FMI path, attributes, and additional cache-key components). | Client secret, certificate/private-key bytes, assertion contents, or claims challenge. |
| C2S user flow | Credential kind (`AT`), client ID, canonical cloud/environment, actual token tenant, stable user identity (normally home account ID), normalized returned scope set, token type/binding key ID when applicable, and explicitly cache-affecting components. | Credential bytes or the authority alias's input spelling. |
| OBO | The C2S dimensions plus an OBO subject partition: assertion hash by default or the configured long-running OBO key. Retain the actual user identity and tenant when available; the override replaces the assertion-hash partition, not the client, cloud, tenant, scopes, or token-binding dimensions. | Raw incoming assertion or claims challenge. |

For app-only acquisition, the tenant must be a specific tenant in a reusable
entry. If the flow has not resolved a tenant, do not merge entries across
tenants; preserve the current public request behavior while resolving or
declining to promote the entry into a shared L1. For user tokens, do not use
`common` or `organizations` as the token tenant: use the actual tenant
associated with the token. In OBO, prefer the validated incoming token's
tenant where the flow contract supplies it; otherwise use the same validated
tenant resolution as token response processing. Never turn a missing tenant
into a wildcard cache hit.

Use home account ID as the primary cross-tenant user identity. Where a flow
has no home account ID but supplies a subject, the fallback must be an
issuer-/tenant-qualified, client-qualified `(tid, sub)` identity; `sub` alone
is pairwise and is not a global user key. If neither identity is safely
available, do not share the token across requests or users.

Scopes are normalized as a case-insensitive set for comparison and
order-independent identity. Preserve the existing superset lookup behavior:
a token may satisfy a request if the returned token scopes contain every
requested non-reserved scope. When several entries satisfy a request, do not
arbitrarily choose one. Preserve the existing overlap-reconciliation rule or
define and test an equivalently safe deterministic policy before changing it.
Do not assume the requested resource string alone captures the scopes Entra
returns.

### Refresh tokens

For existing user refresh tokens, preserve the current semantics:

- Normal refresh token: user identity + canonical environment + client ID.
- Family refresh token: user identity + canonical environment + family ID;
  it remains intentionally shareable across clients.
- OBO refresh token: additionally partition by the assertion hash or
  long-running OBO key, matching the access-token OBO ownership model.

Scopes are not a refresh-token key. Do not add a tenant dimension to existing
user RT identity without proving that it preserves cross-tenant refresh
behavior and serialized-cache interoperability.

Current application-token caches do not save RTs. To leave room for future
principal refresh tokens, represent the credential kind and subject type
explicitly rather than designing an app-token-only key format. A proposed
future principal-RT identity is `(principal type, principal ID, tenant,
cloud/environment, client ID or family ID, credential kind)`, with only
protocol-required dimensions. Keep user RTs and principal RTs in separate
identity namespaces. Do not key either by secret or private-key material.
This is a forward-compatible key-model proposal, not a claim that MSAL
currently supports service-principal RTs.

### ID tokens, accounts, and metadata

- ID token: user identity + canonical environment + client ID + actual tenant
  + credential kind. No scopes. Preserve existing behavior that ID tokens
  are not partitioned by OBO assertion key.
- Account/profile: home account ID + canonical environment + actual tenant.
  Account removal must find and remove all related credential records.
- Application metadata: canonical environment + client ID. It is not a
  credential and must not be accidentally discarded with an access-token LRU
  entry. Bound or separately account for metadata growth.

### Claims and other request parameters

Claims are an acquisition instruction, not a durable identity dimension.
Continue to bypass ordinary cached-token lookup for claims requests in flows
that currently do so; honor explicit cache-bypass/refresh APIs. Do not make a
claims-bearing response globally reusable unless its normal cache identity
and flow semantics permit it. Test each flow before changing this behavior.

Include only request parameters that can change token authorization or
binding, and that MSAL or the caller explicitly marks as cache-affecting.
Examples already supported include additional cache-key components, FMI path,
credential FMI path, and token attributes. Extra query parameters must retain
their explicit `includeInCacheKey` choice. Do not hash every request parameter
by default: that creates accidental misses and may expose sensitive input.

### Cloud and environment aliases

Store one canonical environment/cloud identity using MSAL's existing instance
metadata and preferred-cache rules. Resolve known aliases to the same
environment for lookup, but never merge sovereign clouds or unknown hosts
without authoritative metadata. Preserve multi-cloud behavior and the
original authority for protocol operations. A cache entry's logical identity
must include cloud, even if a legacy callback partition hint does not.

## 4. L1 behavior and memory policy

### Proposed first policy

- One L1 entry represents one independently addressable access token, not all
  tokens for one S2S application/tenant/resource family.
- Use an MSAL-owned recency list or a cache implementation with verified
  size/eviction semantics. A successful cache hit and insertion/update moves
  the entry to most-recently-used. Evict least-recently-used eligible entries
  when the configured budget is exceeded.
- Initial S2S target: **10 MiB across the process-wide MSAL-managed
  application-token L1**, not 10 MiB per `IConfidentialClientApplication`.
  This is a proposed default to validate, not an already committed product
  contract. Make it configurable and allow disabling the managed cache.
- Remove expired entries before live LRU entries where practical. Token
  expiration remains an authentication validity check; expiration is not
  equivalent to memory eviction.
- Update, eviction, expiration, `Clear`, account removal, and failed token
  acquisition must update the entry and accounting state atomically.
- If an individual entry is larger than the configured budget, return the
  acquired token to the caller but do not retain it in L1.
- Size each retained record from an estimator calibrated by runtime and
  architecture, including record/key/collection overhead. The existing
  `TokenCacheSnapshot.EstimateSizeInBytes()` proposed in
  [PR #6296](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/6296)
  can inform this work, but its estimate is not a hard upper bound. Reserve
  explicit headroom and report both accounted bytes and the configured capacity.
- Keep ID tokens, accounts, app metadata, and refresh tokens in explicit
  lifecycle classes. Evict access tokens before user refresh tokens; dropping
  an AT may cause refresh/network work, while dropping an RT can require
  interactive sign-in. Do not evict RTs or user state under pressure until
  product policy, UX, and tests are agreed.

For C2S, use a separately configurable bounded budget or a deliberately
shared process budget with per-account fairness. Do not let one user's token
volume evict every other user's L1 without an explicit policy. Initially,
apply the same entry identity and persistence model but keep user-token LRU
off until the refresh-token, account-removal, OBO, and fairness scenarios
below are satisfied.

### Concurrency and coherence invariants

- Cache lookup, recency update, insert/replace, and eviction are one coherent
  operation; do not hold a global lock across network or persistence I/O.
- Coalesce concurrent cold misses/refreshes by logical token identity where
  protocol semantics permit it. A canceled waiter must not cancel work still
  needed by other waiters. Refresh-token rotation and token-response writes
  must not allow an older response to overwrite newer state.
- Mutations and removals must invalidate or supersede any in-flight L2 read.
  Use per-key generations/versions or an equivalent fencing mechanism so a
  delayed read cannot republish stale state after logout, account removal,
  or a newer token write.
- In-process synchronization is owned by MSAL. Cross-process synchronization
  requires a backend capability (for example CAS, lease, or versioned writes)
  or a documented bounded-staleness policy. `IDistributedCache`'s basic
  get/set/remove operations alone do not provide cross-process transactions
  or invalidation.
- Account removal and `Clear` must remove or tombstone all associated L1 and
  L2 entries. A per-token L2 layout therefore needs a safe secondary index,
  partition generation, or equivalent deletion mechanism; it cannot rely on
  enumerating arbitrary distributed-cache keys.
- Do not silently fall back from a failed persistence write to success-shaped
  behavior that leaves cache layers divergent. Surface provider failures
  according to the existing public operation/callback contract and record
  safe diagnostic telemetry.

## 5. Persistence architecture alternatives

The L1 and L2 roles should remain distinct: L1 is a bounded, process-local
lookup cache; L2 is optional persistence for restart/cross-instance reuse.
Using L2 does not by itself make L1 coherent.

| Design | Benefits | Costs and risks |
|---|---|---|
| **A. MSAL directly depends on `IMemoryCache`/`MemoryCache` and `IDistributedCache`** | Familiar .NET abstractions; easy DI and provider reuse; can accelerate an initial integration; no need to publish MSAL record types. | Adds Microsoft.Extensions dependencies to a cross-target library; ties lifetimes/configuration to host DI; `IDistributedCache` is a blob key/value contract without CAS, enumeration, transactions, or invalidation; backend and MemoryCache eviction details must be validated; coupling the L1 implementation to a framework abstraction does not resolve L1/L2 races. |
| **B. Make `ITokenCacheInternal` or equivalent cache internals public** | Maximum control for providers that need to manipulate tokens directly; can preserve custom storage strategies. | `ITokenCacheInternal` composes implementation details and internal credential/cache-item types. Publicizing it would freeze lookup, mutation, account, locking, and serialization details into a large permanent API; callers could bypass synchronization and invariants. This is the least attractive option. `ITokenCache` itself is already public, but it exposes notifications/serialization, not safe per-token mutation. |
| **C. Recommended: MSAL-owned cache engine plus a narrow optional opaque persistence contract** | MSAL retains identity, lookup, LRU, and synchronization ownership; providers only persist versioned opaque MSAL records; no token-item model becomes public; can adapt `IDistributedCache` where its weaker consistency is acceptable. | Requires designing a small new contract, a compatibility adapter for existing callbacks, and explicit capability handling for atomic backends. Does not eliminate the cost of provider failures or L2 latency. |

The recommended contract is a small **proposal**, not a final public API. It
should exchange opaque bytes and stable opaque keys, never
`MsalAccessTokenCacheItem` or mutable dictionaries. Conceptually it needs:

- read by opaque record/partition key, returning payload plus optional version;
- write with expiry and optional expected version/fencing token;
- remove or tombstone by key/generation;
- cancellation and awaited completion;
- optional capability declaration for CAS/lease/invalidation.

Keep the simplest provider path possible. A basic `IDistributedCache` adapter
may implement best-effort last-writer-wins persistence, but MSAL must not
describe it as linearizable or cross-process synchronized. Strong coherence
must require an explicit backend capability or a narrowly scoped lock service.
Never serialize a complete user cache and overwrite it after every individual
token change without concurrency protection; concurrent writers can lose
unrelated tokens.

An application that already uses `SetBeforeAccess`/`SetAfterAccess` continues
to work unchanged. Those callbacks remain the compatibility path for a
whole-cache serializer; new per-entry persistence must not change their
meaning or silently invoke both mechanisms against the same backing store.

## 6. Public API and ecosystem compatibility

- Do not make `ITokenCacheInternal`, `ITokenCacheAccessor`, or mutable cache
  records public.
- Keep `ITokenCache` callbacks and serialization APIs supported. Add any
  provider registration/configuration as an additive API, or integrate it
  through an existing builder/host abstraction without requiring
  Microsoft.Identity.Web or MISE code changes.
- Preserve `SerializeMsalV3`/`DeserializeMsalV3`, unknown top-level and
  per-record fields, account APIs, removal semantics, ADAL interop where
  applicable, and mobile/broker-specific accessors.
- Keep the interoperable V3 representation independent of L1 eviction
  metadata. New private persistence envelopes must be versioned and must
  round-trip through V3 without dropping unknown fields.
- Do not alter the existing `suggestedCacheKey` value or its documented
  interpretation in the first release. If a future API needs a per-token key,
  add a separate typed opaque key rather than silently changing the old
  callback's partition key.
- Make old and new MSAL versions coexist safely. Read existing V3 data;
  write additive/compatible data; dual-read and migrate if a private L2
  namespace changes. Do not delete the old namespace until rollback and
  downgrade behavior is proven.
- Microsoft.Identity.Web and MISE must not need a source change, warning
  suppression, new callback ordering, or a new interpretation of existing
  keys. Validate this with public API compatibility and integration tests
  before enabling a new default.

No public API or serialization break is needed to ship an internal bounded
S2S L1. However, evicting an entry can turn a cache hit into token-endpoint
traffic, so the operational behavior is observable. Start opt-in and measure
before enabling by default.

## 7. Rollout

1. **Instrument and reproduce.** Measure retained bytes/count by cache class,
   app/client and tenant using privacy-safe aggregation; reproduce leaks,
   stale-L1, lost-update, logout, and refresh races in real provider
   topologies. Do not log tokens, raw cache keys, user IDs, claims, or
   assertions.
2. **S2S identity tests and internal LRU.** Use the current V3 serializer
   and existing acquisition behavior. Introduce per-access-token internal
   keys and a configurable, initially opt-in 10 MiB application L1. Test
   multiple resources, tenants, clouds, FMI paths, attributes, PoP key IDs,
   scope expansion, expiration, and overlapping scopes.
3. **Observe before defaulting.** Canary hit rate, memory, evictions, token
   endpoint requests, latency, failures, duplicate requests, and cache
   contention. Verify the budget and estimates on .NET Framework and .NET,
   x64 and supported architectures. A memory reduction that causes material
   token-endpoint load or latency is not an acceptable success.
4. **Add optional L2 provider.** Add only after the consistency contract is
   clear. Test two processes, concurrent same-key and different-key writes,
   provider timeout/failure, stale read after write/removal, restart,
   deployment rollback, and older/newer MSAL sharing data.
5. **Add user tokens.** Enable user LRU only after account identity, actual
   tenant, OBO assertion/custom key, regular and family RT, ID-token,
   account-removal, refresh-token rotation, public-client, and fairness tests
   pass. Keep user eviction policy separate from the S2S first phase.
6. **Default and future principal tokens.** Make bounded app-token caching
   the default only after compatibility and production evidence supports it.
   Add service-principal refresh tokens only with an identity contract,
   explicit tests, and versioned cache interoperability. Never infer that
   current app caches can store them.

## 8. Required validation

### S2S

- Same client, tenant, and scopes hits; different resources/scopes do not
  collide; `/.default` and returned scopes are normalized safely.
- Different client IDs, tenants, clouds, authentication schemes/PoP key IDs,
  attributes, FMI paths, and explicit cache-key components do not collide.
- Credential rotation for the same client does not create secret-bearing
  keys or unintended duplicate entries.
- Claims, force refresh, explicit token-hash refresh, expiration, proactive
  refresh, cancellation, and overlapping writes preserve current behavior.
- One hundred-thousand-entry or larger stress tests prove bounded memory,
  bounded lookup cost, and correct eviction under concurrent load.

### C2S and OBO

- Same home account across two tenants remains two tenant-specific access
  tokens; distinct users never collide.
- `(tid, sub)` fallback is issuer- and client-qualified; missing identity
  cannot become a wildcard hit.
- OBO assertion hash isolates assertions; a configured long-running key is
  stable and replaces only the assertion partition dimension.
- A returned scope superset may satisfy a subset request; ambiguous matching
  does not select an arbitrary token.
- Normal RT, family RT, ID token, account, and app metadata identities remain
  distinct. Account removal removes every related credential and index.
- Evicting an AT can renew through an available RT; RT eviction behavior is
  explicit and does not silently cause unexpected interactive requirements.

### L1/L2, compatibility, and performance

- Check LRU order, byte-accounting changes, no oversized-entry retention,
  expiry cleanup, and update/evict/remove atomicity.
- Use deterministic barriers/fault injection to prove that delayed reads
  cannot resurrect stale entries after newer writes or removal.
- Round-trip unknown V3 fields across old/new versions and preserve portable
  cache payloads.
- Verify public API compatibility and run existing Microsoft.Identity.Web
  and MISE package/integration tests without requiring their code to change.
- Benchmark warm hits, cold misses, writes, L2 hit/miss, user lookups, and
  eviction at 1/2/3/10/100/1,000+ entries; measure latency distributions,
  allocations, retained heap, GC, and downstream token-endpoint rate.
- Test supported target frameworks, x64 and x86 where supported, mobile and
  broker paths, and trimmed/AOT builds before advertising those combinations.

## 9. Decisions proposed for review

1. Accept MSAL-owned LRU and a narrow opaque persistence provider; do not
   expose `ITokenCacheInternal`.
2. Start with S2S access tokens and a configurable 10 MiB process-wide
   application-token L1 target, opt-in until canary evidence supports a
   default.
3. Keep C2S/OBO on the same logical key engine, but defer user-token eviction
   until RT/account/OBO policy and fairness are tested.
4. Treat `IDistributedCache` as a possible storage adapter, not as the
   synchronization or coherence design. Require explicit consistency
   capabilities for stronger guarantees.
5. Preserve current V3 and callback contracts; add new APIs only when the
   opaque provider contract is stable and downstream-compatible.

Unresolved identity-platform details (notably future principal RT issuance
and which flows can safely derive `(tid, sub)`) should be settled with
protocol evidence before those paths ship. They do not block the S2S
access-token LRU phase.
