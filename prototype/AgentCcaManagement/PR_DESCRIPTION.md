# Prototype: Agent CCA Instance Management Strategies

## Background

The **agent identity flow** (FMI/FIC) requires a "multi-CCA" pattern: one Blueprint CCA that owns the real certificate credential, plus **N Agent CCAs** — one per agent app ID — each with its own in-memory token cache for natural isolation. This pattern is documented in our [Agent Identity MSAL Developer Guide](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/prototype/AgentCcaManagement/README.md) and is now implemented in ID Web via [PR #3842](https://github.com/AzureAD/microsoft-identity-web/pull/3842).

The open question is: **how should the collection of N Agent CCAs be bounded in a long-running service?**

Today, both the guidance doc's `AgentTokenService` example and ID Web's implementation use an unbounded `ConcurrentDictionary<string, IConfidentialClientApplication>`. In most deployments this is fine (typically 1–3 agents per service), but:
- Platform/orchestrator services that host many agents could accumulate hundreds of CCA instances
- Each CCA holds an in-memory token cache that grows with user count
- There is no mechanism to reclaim memory from agents that are no longer active

This PR provides **three side-by-side prototype implementations** comparing different strategies, with unit tests that validate correctness, concurrency safety, and eviction behavior — all without hitting real Entra ID endpoints.

---

## The Three Strategies

### 1. Unbounded (Baseline)

**What it is:** A simple `ConcurrentDictionary` with double-checked locking via per-key semaphores. Exactly what ID Web and the guidance doc use today.

**When it makes sense:**
- Deployments with a known, small number of agents (e.g., 1–5)
- Short-lived processes (CLI tools, Azure Functions with short cold-start cycles)
- When simplicity and zero operational surprises outweigh memory concerns

**Developer experience:** Zero configuration. Create the service, call `GetOrCreateAgentCcaAsync`, done. No expiration windows to tune, no background timers, no eviction callbacks to handle.

**Limitation:** The dictionary grows forever. A service running for months with rotating agents will accumulate stale CCA instances whose tokens have long expired but whose memory is never reclaimed.

---

### 2. Sliding Expiration (MemoryCache)

**What it is:** Uses `Microsoft.Extensions.Caching.Memory.MemoryCache` with a configurable sliding expiration window and hard size cap. Entries that haven't been accessed within the window are evicted; entries are also evicted when the size limit is exceeded.

**When it makes sense:**
- Services that already use `Microsoft.Extensions.*` (ASP.NET Core, ID Web)
- When you want both time-based and size-based eviction without writing custom logic
- When eviction callbacks are needed to clean up companion state (e.g., account ID maps)

**Developer experience:**
- Configure `MaxAgentCcas` and `SlidingExpiration` at startup
- Register an `OnEviction` handler if companion cleanup is needed
- Otherwise transparent — `GetOrCreateAgentCcaAsync` handles everything

**Pros:**
- Built-in to .NET; battle-tested infrastructure
- Sliding expiration aligns with token lifetimes (idle CCA ≈ expired tokens ≈ cheap eviction)
- Eviction callbacks enable coordinated cleanup of related state
- Size cap prevents burst scenarios from growing unbounded

**Cons:**
- **Lazy eviction timing** — MemoryCache does not eagerly evict expired entries. Expiration only triggers during subsequent access or background GC compaction scans. This means `CachedCcaCount` may temporarily exceed expectations, and eviction callbacks fire at unpredictable times. (Our tests demonstrate this directly — the time-based eviction callback test was unreliable and had to be replaced with a size-cap-triggered test.)
- **Stampede problem** — `GetOrCreateAsync` doesn't prevent multiple concurrent callers from all executing the factory for the same missing key. A semaphore layer is still required on top of MemoryCache.
- **GC-triggered surprises** — Under memory pressure, MemoryCache may evict entries earlier than the configured window, which could cause unexpected cache rebuilds mid-request.

---

### 3. Timestamp Sweep (ConcurrentDictionary + Background Timer)

**What it is:** Keeps the familiar `ConcurrentDictionary` but wraps values with a last-accessed timestamp. A background `Timer` periodically sweeps and removes entries that exceed the idle threshold. A hard size cap enforces oldest-first eviction synchronously when the limit is hit during `GetOrCreate`.

**When it makes sense:**
- Libraries that want full control over eviction timing and behavior
- Scenarios where predictability is more important than automation
- When the MemoryCache lazy-eviction behavior is unacceptable (e.g., strict memory budgets)

**Developer experience:**
- Configure `MaxAgentCcas`, `SlidingExpiration`, and `SweepInterval`
- Optionally call `Sweep()` manually in tests or at specific lifecycle points
- Register `OnEviction` for companion cleanup

**Pros:**
- **Predictable eviction** — entries are only removed during the sweep cycle or when the size cap is hit. No surprises from GC pressure or lazy scans.
- **Deterministically testable** — call `Sweep()` in tests and assert results immediately. All timestamp sweep tests pass on first run without timing workarounds.
- **Familiar pattern** — it's still a `ConcurrentDictionary` underneath. Minimal conceptual overhead for developers already using the unbounded pattern.
- **Lock-free reads** — accessing an existing CCA only requires a `Volatile.Write` of the timestamp (vs MemoryCache's internal locking).

**Cons:**
- **You maintain the sweep logic** — it's simple (~30 lines) but it's yours to own and test.
- **Between-sweep staleness** — expired entries persist until the next sweep runs. With a 30-minute sweep interval, a CCA that expires at T+1min lingers until T+30min.
- **Oldest-first eviction on size cap** is O(N) — it iterates the dictionary to find the oldest entry. Fine for N < 1000 (the expected range), but not ideal if N could be very large.

---

## Why Sliding Expiration Over Pure LRU?

All three bounded strategies use **time-based** eviction rather than access-order (LRU). This is deliberate:

| Factor | Time-based | LRU |
|--------|-----------|-----|
| Eviction cost correlation | High — idle CCA ≈ expired tokens ≈ cheap to rebuild | Low — evicts "least recent" regardless of cached token freshness |
| Alignment with token lifetimes | Natural — tokens expire on a time basis (1h AT, 24h cache entries) | None — access recency doesn't predict token validity |
| Read-path contention | None (or one atomic write for timestamp) | Must update ordering data structure on every access |
| Implementation complexity | Simple timestamp comparison | Requires doubly-linked list or similar ordered structure |

---

## Recommendations

### For ID Web (internal SDK)
The **Timestamp Sweep** strategy is likely the best fit:
- ID Web already uses `ConcurrentDictionary` for multiple CCA pools — this is a minimal delta
- Predictable eviction behavior avoids support issues from MemoryCache's lazy timing
- The `Sweep()` method is trivially testable with ID Web's existing mock HTTP patterns
- The eviction event integrates cleanly with `_agentUserFicAccountIds` cleanup

### For customer guidance (public documentation)
Present all three with clear selection criteria:

| Your scenario | Recommended strategy |
|---------------|---------------------|
| Small, fixed set of agents (1–10) | **Unbounded** — simplest, no configuration needed |
| Long-lived service with moderate agent churn | **Timestamp Sweep** — predictable, testable, familiar dictionary pattern |
| ASP.NET Core app already using `IMemoryCache` / DI | **Sliding Expiration** — leverages existing infrastructure, minimal new code |

Optionally, MSAL .NET itself could ship a built-in `BoundedCcaRegistry` utility class (in a helper package or in the core library) so that neither ID Web nor customers need to reinvent this pattern.

---

## Test Results

All **25 unit tests** pass across the three strategies:
- **15 shared tests** — cache hit/miss, instance identity, count tracking, concurrency safety, input validation (5 tests × 3 strategies)
- **4 MemoryCache-specific tests** — size cap, sliding expiration, touch-to-reset, eviction callback
- **6 Timestamp Sweep-specific tests** — oldest-first eviction, touch-keeps-alive, manual sweep, background sweep, sweep-returns-zero, concurrency under size cap

The tests use real MSAL `ConfidentialClientApplication` instances (with static assertion callbacks) — no mocking of MSAL internals. This validates that the strategies work with actual CCA objects and their threading characteristics.

---

## How to Run

```bash
cd prototype/AgentCcaManagement
dotnet test           # Run all 25 tests
dotnet run --project UnboundedStrategy          # Visual demo
dotnet run --project SlidingExpirationStrategy  # Visual demo with eviction
dotnet run --project TimestampSweepStrategy     # Visual demo with sweep
```

---

## Open Questions for Discussion

1. **Should MSAL .NET ship this as a utility?** A `BoundedCcaRegistry` in the core library (or a helper package) would prevent every consumer from building their own.
2. **Should the guidance doc default to bounded?** Currently the `AgentTokenService` example is unbounded. Should we change the default to timestamp-sweep with a note that unbounded is acceptable for small deployments?
3. **Eviction granularity**: Should we evict at the CCA level (losing all cached tokens for that agent), or could MSAL support per-agent cache partitioning within a single CCA to enable finer-grained eviction?
4. **Companion state cleanup**: The `_agentUserFicAccountIds` map in ID Web needs coordinated cleanup. Should the eviction callback pattern be formalized (as shown in these prototypes), or should the account map be co-located inside the CCA wrapper?
