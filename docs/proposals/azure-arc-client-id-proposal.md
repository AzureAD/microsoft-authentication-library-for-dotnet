# Azure Arc UAMI Support: Why the Client SDK Should Not Gate the Request

## Summary

This proposal recommends enabling Azure Arc user-assigned managed identity (UAMI) request handling in the client SDK by replacing the unconditional Azure Arc UAMI guard with agent-version-gated forwarding. MSAL forwards the identity selector (`client_id`, `object_id`, or `mi_res_id`) only when `ARC_AGENT_VERSION` shows a supported agent (>= 1.66). Otherwise it throws `user_assigned_managed_identity_not_supported` instead of sending the request - an old agent would ignore the selector and return the system-assigned token.

The result was clear:

- A request without an identity selector successfully returned a token for the Arc-enabled server's system-assigned managed identity.
- A request with a random, unassigned `client_id` reached HIMDS and returned an authoritative service response:

```text
HTTP 404 NotFound
error: identity_not_found
error_description: Identity not found: error acquiring access token:
requested identity was not found
```

This demonstrates that HIMDS understands the `client_id` selector and is the correct component to determine whether the requested identity exists, is assigned to the machine, and can be used.

The client SDK should therefore not reject Azure Arc UAMI requests on agents that advertise UAMI support - but it must still fail closed on older agents, which silently ignore the selector and return the system-assigned identity (see [Compatibility Considerations](#compatibility-considerations)).

---

## What We Tested

### 1. System-assigned identity request

We called the Azure Arc local managed identity endpoint without a user-assigned identity selector:

```text
http://localhost:40342/metadata/identity/oauth2/token
    ?resource=https%3A%2F%2Fmanagement.azure.com%2F
    &api-version=2020-06-01
```

After completing the normal Azure Arc challenge flow, HIMDS returned a valid access token.

This confirmed that the local endpoint and authentication flow were working correctly.

### 2. User-assigned identity request

We then added a random `client_id`:

```text
http://localhost:40342/metadata/identity/oauth2/token
    ?resource=https%3A%2F%2Fmanagement.azure.com%2F
    &api-version=2020-06-01
    &client_id=427a9bd4-f55a-4766-b29e-d8e662d49957
```

The request completed the Azure Arc challenge flow and reached the authenticated token request.

HIMDS returned:

```json
{
  "error": "identity_not_found",
  "error_description": "Identity not found: error acquiring access token: requested identity was not found",
  "error_codes": [404]
}
```

This is the expected result for an identity that is not assigned to the Arc-enabled server.

The important finding is not that the random identity failed. The important finding is that HIMDS accepted and processed the identity selector, then returned the correct service-side error.

---

## What This Proves

The test shows that Azure Arc HIMDS:

- Recognizes the `client_id` selector.
- Attempts to resolve the requested identity.
- Returns a specific `identity_not_found` error when the identity is unavailable.
- Is capable of making the authoritative decision about identity availability.

The selector is not ignored, and the request is not rejected merely because it contains a user-assigned identity parameter.

This means the existing client-side Azure Arc restriction is no longer the correct place to enforce support.

---

## Why the Client SDK Should Not Gate This

A client SDK does not have enough information to determine whether a user-assigned identity is supported for a particular Arc-enabled server.

Support can depend on:

- The installed Azure Connected Machine Agent version.
- Whether the machine is enrolled in the private preview.
- Whether the required TPM or vTPM capability is available.
- Whether the requested identity is assigned to that machine.
- Which identity selector is being used.
- Current HIMDS capabilities and service rollout state.

A hardcoded SDK check becomes stale as the service evolves. It also prevents newer agents and preview-enabled environments from using capabilities that the local service already supports.

The SDK should instead:

1. Validate only request syntax and mutually exclusive selector usage.
2. Forward the selected identity parameter to HIMDS.
3. Allow HIMDS to determine whether the identity exists and is supported.
4. Surface the resulting service error without replacing it with a client-side assumption.

This produces more accurate behavior and better diagnostics.

For example, these are materially different failures:

```text
identity_not_found
requested identity was not found
```

```text
unsupported identity selector
```

```text
managed identity endpoint unavailable
```

```text
invalid request
```

The service is in the best position to distinguish these cases.

---

## Existing Client-Side Guard

The Azure Arc managed identity implementation previously rejected user-assigned identities before constructing or sending the request.

That restriction originated when Azure Arc supported only the machine's system-assigned identity - and, as confirmed with the Arc team, it also guarded against a real failure mode: a pre-UAMI agent silently ignores the identity selector and returns the system-assigned token (see [Compatibility Considerations](#compatibility-considerations)).

The restriction is therefore not purely historical. What is now outdated is applying it **unconditionally**. On agents that advertise UAMI support through `ARC_AGENT_VERSION`, the guard should give way to forwarding the selector.

Keeping the restriction unconditionally now has several disadvantages:

- It blocks supported preview scenarios.
- It prevents service-side capability detection.
- It hides the authoritative HIMDS error.
- It requires an SDK update every time Arc expands identity support.
- It creates inconsistent behavior across MSAL, Azure SDK, Azure CLI, and other language implementations.

---

## Proposed SDK Behavior

Replace the unconditional Azure Arc-specific guard with agent-version-gated forwarding.

When a user-assigned identity is configured **and** `ARC_AGENT_VERSION` indicates a supported agent (>= 1.66), forward the corresponding selector to HIMDS. MSAL supports all three selectors - `client_id`, `object_id`, and `mi_res_id`.

| Managed identity selection | HIMDS parameter | MSAL support |
|---|---|---|
| Client ID | `client_id` | Enabled, gated on `ARC_AGENT_VERSION` >= 1.66 |
| Object ID | `object_id` | Enabled, gated on `ARC_AGENT_VERSION` >= 1.66 |
| Azure resource ID | `mi_res_id` | Enabled, gated on `ARC_AGENT_VERSION` >= 1.66 |

When `ARC_AGENT_VERSION` is missing or below the supported version, MSAL rejects the UAMI request with the existing `user_assigned_managed_identity_not_supported` error rather than sending a selector the agent would silently ignore.

Example (supported agent):

```text
GET /metadata/identity/oauth2/token
    ?resource=<encoded-resource>
    &api-version=2020-06-01
    &client_id=<encoded-client-id>
```

The SDK should continue to enforce client-side rules that are independent of Arc capability, such as ensuring that only one identity selector is supplied.

It should not decide whether the selected identity is assigned or supported.

---

## Compatibility Considerations

### Older agents do not fail closed - they silently return the system-assigned identity

A direct check with the Azure Arc team corrected an earlier assumption in this proposal. Pre-UAMI HIMDS agents do **not** reject an unknown identity selector. They **ignore** `client_id`, `object_id`, and `mi_res_id` and return a token for the machine's **system-assigned** managed identity on every request.

This is confirmed behavior, not a theoretical edge case, and it was observed on both `api-version=2019-11-01` and `api-version=2020-06-01`. It is also the original reason the client SDKs filtered out Arc UAMI requests.

The consequence is a silent identity-confusion failure: a caller that requests UAMI `A` receives a system-assigned token and never learns the selector was dropped. The token is opaque to MSAL, so the SDK cannot detect the substitution after the fact. "Let the service decide" does not help here, because a pre-UAMI service makes the wrong decision silently instead of returning an error.

Because of this, MSAL cannot unconditionally forward the selector. It must first confirm that the local agent actually supports UAMI.

### Agent-version detection via `ARC_AGENT_VERSION`

To detect support without adding an HTTP round trip, the Azure Arc agent now exposes its version through a new environment variable, `ARC_AGENT_VERSION`, set by the HIMDS process at startup (Azure Arc agent change: "Expose agent version via ARC_AGENT_VERSION env var").

MSAL uses this variable as the gate:

- If `ARC_AGENT_VERSION` is present and the version is **>= 1.66** (the first `azcmagent` version with UAMI support), MSAL forwards the requested selector to HIMDS.
- If `ARC_AGENT_VERSION` is missing or **< 1.66**, MSAL throws `user_assigned_managed_identity_not_supported` and does not send the request. It does not just drop the selector - a request with no selector returns the system-assigned token.

For private-preview customers on an agent that supports UAMI but does not yet set the variable, the value can be exported via script until the capability ships in the next agent release.

This keeps detection purely local - no probe request and no version endpoint - and ensures MSAL never sends a selector to an agent that would silently ignore it.

### HIMDS API version

The HIMDS API version is aligned across language implementations at `2020-06-01`. The API version alone does **not** gate UAMI support; the agent version does. The same silent system-assigned fallback was observed on both `2019-11-01` and `2020-06-01`, so only the agent build determines whether the selector is honored.

---


## Ecosystem and Rollout Impact

MSAL is the authentication engine used beneath several higher-level libraries and tools. Enabling the Azure Arc UAMI request flow in MSAL SDKs is therefore not limited to direct MSAL callers.

As support is added across MSAL.NET, MSAL Python, MSAL Java, MSAL JavaScript/Node, and MSAL Go, higher-level SDKs and tools that depend on those libraries can begin inheriting the capability through their normal dependency update cycles. This includes frameworks and products such as Microsoft.Identity.Web, MISE, Azure SDK integrations, Azure CLI, PowerShell modules, and other managed identity consumers.

This creates an important sequencing advantage:

1. Remove the obsolete client-side restriction in the foundational MSAL SDKs.
2. Publish preview packages that understand and forward the Arc identity selectors.
3. Allow higher-level SDKs and tools to validate and adopt those versions incrementally.
4. Complete the ecosystem work before Azure Arc UAMI becomes broadly available.

The goal should be that by the time Azure Arc completes its planned `client_id` support rollout later this year, the SDK ecosystem is already capable of sending the request. Customers should not have to wait for a second wave of client-library changes after the Arc capability is available.

This does not mean every higher-level SDK must ship at the same time. It means the foundational libraries should remove the blocking assumption early enough that downstream adoption can proceed in parallel with the Azure Arc rollout.

A hardcoded client-side gate reverses this sequencing. Even after the Arc agent supports UAMI, every SDK and higher-level dependency would remain blocked until it is separately updated. Removing the gate now allows service readiness and SDK readiness to converge rather than happen serially.

---

## Proposed Change Across SDKs

We should review all managed identity SDK implementations that contain an Azure Arc-specific UAMI prohibition.

For each implementation:

1. Replace the hardcoded Azure Arc UAMI rejection with `ARC_AGENT_VERSION`-gated forwarding (fail closed when the variable is absent or below the supported version).
2. Confirm that identity selectors are forwarded correctly.
3. Preserve selector-specific cache partitioning.
4. Preserve the existing Azure Arc challenge-response authentication flow.
5. Surface HIMDS response status, error code, and description.
6. Add unit tests for all supported selectors.
7. Add an end-to-end test using a preview-enabled Arc machine and an actually assigned UAMI.

This should include, as applicable:

- MSAL.NET
- MSAL Python
- MSAL Java
- MSAL JavaScript/Node
- MSAL Go
- Azure Identity SDK integrations
- Azure CLI managed identity flows

The rollout can be prioritized by current private-preview customer requirements rather than requiring all languages to ship simultaneously.

---

## Recommended Tests

### Positive tests

- System-assigned identity without a selector.
- UAMI selected using `client_id`.
- UAMI selected using `object_id`.
- UAMI selected using `mi_res_id`.
- Token claims correspond to the selected UAMI.
- Separate cache entries are maintained for system-assigned identity and each UAMI.

### Negative tests

- Valid but unassigned client ID returns `identity_not_found`.
- Valid but unassigned object ID returns `identity_not_found`.
- Valid but unassigned resource ID returns `identity_not_found`.
- Malformed selector value returns the HIMDS validation error.
- Multiple selectors are rejected by the SDK as an invalid request.
- Unsupported or older agents return their native HIMDS error.
- Missing challenge file permissions produce the existing Arc authentication failure.

---

## Recommendation

Replace the unconditional client-side Azure Arc UAMI gate with agent-version-gated forwarding.

On a supported agent (`ARC_AGENT_VERSION` >= 1.66), the SDK should forward the caller's identity selector (`client_id`, `object_id`, or `mi_res_id`) and allow HIMDS to make the authoritative decision. On an unsupported or unknown agent, the SDK should keep rejecting UAMI so it never receives a silently substituted system-assigned token.

This preserves the desired boundary on supported agents:

```text
SDK responsibility:
Confirm agent support via ARC_AGENT_VERSION, then construct and send the valid managed identity request.

HIMDS responsibility:
Determine whether the requested identity exists, is assigned, and is supported.
```

On supported agents, the `identity_not_found` response from HIMDS is exactly the behavior we should preserve and expose. The client-side check remains only as a fail-closed guard for agents that would otherwise ignore the selector.

---

## Decision Requested

Approve the following direction:

- Replace the unconditional Azure Arc UAMI prohibition with agent-version-gated forwarding, using the `ARC_AGENT_VERSION` environment variable (supported when >= 1.66).
- Forward the requested selector (`client_id`, `object_id`, or `mi_res_id`) to HIMDS on supported agents.
- On agents missing `ARC_AGENT_VERSION` or below 1.66, throw an error instead of sending the request, so MSAL never silently gets the system-assigned token.
- Use HIMDS errors as the source of truth for identity availability on supported agents.
- Keep only protocol-independent client validation (for example, mutually exclusive selectors) in the SDK.
- Validate the positive UAMI flow on the Arc private-preview environment before merging to public release branches.
- Prepare the foundational MSAL SDKs early enough for higher-level SDKs and tools to adopt the capability before Azure Arc completes its broader `client_id` rollout later this year.
