# Azure Arc UAMI Support: Why the Client SDK Should Not Gate the Request

## Summary

This proposal recommends enabling Azure Arc user-assigned managed identity request handling in the client SDK by removing the existing Azure Arc-specific guard and forwarding the requested identity selector to the local HIMDS token endpoint.

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

The client SDK should therefore not reject Azure Arc UAMI requests before sending them to HIMDS.

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

That restriction appears to have originated from older Azure SDK behavior, when Azure Arc supported only the machine's system-assigned identity.

It was a historical compatibility assumption rather than a requirement of the current HIMDS request protocol.

Keeping the restriction now has several disadvantages:

- It blocks supported preview scenarios.
- It prevents service-side capability detection.
- It hides the authoritative HIMDS error.
- It requires an SDK update every time Arc expands identity support.
- It creates inconsistent behavior across MSAL, Azure SDK, Azure CLI, and other language implementations.

---

## Proposed SDK Behavior

Remove the Azure Arc-specific guard that rejects user-assigned managed identity configurations.

When an identity selector is provided, forward it to HIMDS using the corresponding query parameter:

| Managed identity selection | HIMDS parameter |
|---|---|
| Client ID | `client_id` |
| Object ID | `object_id` |
| Azure resource ID | `mi_res_id` |

Example:

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

Removing the guard should remain compatible with older Arc agents.

For an older agent that does not support the selector, HIMDS will return its own error response. The SDK can surface that response through the existing managed identity exception path.

This is preferable to a permanent SDK restriction because:

- Newer agents can use the capability immediately.
- Older agents continue to fail safely.
- The returned error reflects the actual local agent behavior.
- No agent-version matrix needs to be embedded in every SDK.

The HIMDS API version should also be aligned across language implementations. Based on the latest Arc team guidance and our successful direct test, the target version is:

```text
2020-06-01
```

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

1. Remove the hardcoded Azure Arc UAMI rejection.
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

Remove the client-side Azure Arc UAMI gate.

The SDK should forward the caller's identity selector and allow HIMDS to make the authoritative decision.

Our direct execution already confirms the desired boundary:

```text
SDK responsibility:
Construct and send the valid managed identity request.

HIMDS responsibility:
Determine whether the requested identity exists, is assigned, and is supported.
```

The `identity_not_found` response from HIMDS is exactly the behavior we should preserve and expose, rather than replacing it with a client-side blanket rejection.

---

## Decision Requested

Approve the following direction:

- Remove Azure Arc-specific UAMI prohibitions from client SDKs.
- Forward `client_id`, `object_id`, or `mi_res_id` to HIMDS.
- Use HIMDS errors as the source of truth for identity availability.
- Keep only protocol-independent client validation in the SDK.
- Validate the positive UAMI flow on the Arc private-preview environment before merging to public release branches.
- Prepare the foundational MSAL SDKs early enough for higher-level SDKs and tools to adopt the capability before Azure Arc completes its broader `client_id` rollout later this year.
