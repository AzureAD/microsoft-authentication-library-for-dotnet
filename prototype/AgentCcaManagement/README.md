# Agent CCA Instance Management — Prototype Comparison

Proof-of-concept samples comparing strategies for bounding the internal dictionary of `IConfidentialClientApplication` instances in the agent identity (FMI/FIC) multi-CCA pattern.

## Context

The agent identity flow requires **N separate CCA instances** (one per agent app ID), each with its own in-memory token cache for natural isolation. The question is: how should we manage the lifecycle of these instances in a long-running service?

See:
- [Agent Identity MSAL Developer Guide](../../../Tools/AgentIDs_MSAL_FICAndFMI_Guide.md) — the multi-CCA pattern
- [ID Web PR #3842](https://github.com/AzureAD/microsoft-identity-web/pull/3842) — current unbounded implementation

## Strategies

| Strategy | Eviction Trigger | Size Bound | Key Advantage | Key Tradeoff |
|----------|-----------------|------------|---------------|--------------|
| **Unbounded** (baseline) | Never | None | Simplest; no eviction surprises | Unbounded memory growth |
| **SlidingExpiration** | Idle time + size cap | Yes (MemoryCache `SizeLimit`) | Built-in .NET; automatic expiration | Eviction timing is lazy/unpredictable; stampede problem needs semaphore workaround |
| **TimestampSweep** | Periodic background sweep + size cap on add | Yes (manual check) | Predictable eviction; familiar dictionary pattern | You maintain sweep logic; between sweeps stale entries persist |

## Structure

```
AgentCcaManagement/
├── Shared/                         # Common interface, options, mock factory
├── UnboundedStrategy/              # Baseline: ConcurrentDictionary, no eviction
├── SlidingExpirationStrategy/      # MemoryCache + sliding expiration + size cap
├── TimestampSweepStrategy/         # ConcurrentDictionary + timestamps + Timer sweep
├── AgentCcaManagement.Tests/       # Shared unit tests (all strategies) + strategy-specific
└── AgentCcaManagement.sln
```

## Build & Test

```bash
cd prototype/AgentCcaManagement
dotnet build
dotnet test
```

Run individual demos:
```bash
dotnet run --project UnboundedStrategy
dotnet run --project SlidingExpirationStrategy
dotnet run --project TimestampSweepStrategy
```

## Why Not LRU?

Sliding expiration (time-based) is likely a better fit than pure LRU for CCA instances because:

1. **Eviction cost correlates with token freshness, not access recency.** A CCA idle for hours has mostly-expired tokens — evicting it is nearly free. A recently-used CCA with many fresh user tokens is expensive to evict regardless of order.
2. **Token lifetimes are time-based** (typically 1h access tokens). Time-based eviction naturally aligns with the underlying value decay.
3. **LRU requires ordering on every access**, adding contention to what is otherwise a lock-free `ConcurrentDictionary` read path.

## Design Questions for Discussion

1. Should this be built into MSAL .NET itself (a reusable "bounded CCA registry") rather than each consumer (ID Web, custom apps) implementing it independently?
2. Should eviction be configurable by the app developer, or should we pick sensible defaults and not expose the knob?
3. For the companion `_agentUserFicAccountIds` map: should it be owned by the same bounded structure, or cleaned up via eviction callbacks?
4. Is the MemoryCache `SizeLimit` behavior (lazy compaction, not strict) acceptable, or do we need hard guarantees?
