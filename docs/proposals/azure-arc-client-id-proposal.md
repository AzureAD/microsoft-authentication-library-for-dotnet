# Azure Arc UAMI Support: Client SDK Design

## Summary

Azure Arc is adding support for user-assigned managed identities (UAMI) through the local HIMDS token endpoint. Today the client SDK blocks UAMI requests on Azure Arc, because an older HIMDS agent silently ignores the identity selector and returns the machine's system-assigned identity (SAMI) token.

This proposal removes that block and replaces it with a per-request check on the token response: HIMDS returns the identity it actually used, and the SDK returns the token only when that confirms the requested identity was honored. When the confirmation is missing (an older agent), the SDK fails the request instead of returning the wrong identity.

This needs no environment variable, no extra network call, and no new endpoint. The identity fields (`client_id`, `object_id`, `msi_res_id`) already exist in the published IMDS response schema, so HIMDS returning them keeps it consistent with IMDS.

## The problem

- The caller asks for a specific user-assigned identity.
- On an older Azure Arc agent, HIMDS ignores the `client_id` / `object_id` / `msi_res_id` selector and returns a token for the system-assigned identity.
- The access token is opaque to the SDK, so the caller cannot tell they got the wrong identity. They run as the machine identity, with different permissions, and find out late or never.

This silent substitution is why the SDK blocks Azure Arc UAMI today. We saw the same class of break in IMDS when the default identity flipped from UAMI to SAMI.

## Why not detect capability another way

Two alternatives were considered and rejected:

- Environment variable (agent version): hard to roll out reliably. On Linux there is no system-wide way to guarantee every process sees a new variable, already-running processes keep the old value, and the value can lag an agent upgrade for a long time. That leads to "I upgraded and it still doesn't work" incidents.
- Extra probe call, new endpoint, or new header: adds latency or a new protocol surface we want to avoid. The SDK also treats the access token as opaque, so it cannot read the identity from inside the token.

## Detection: the used identity in the token response

HIMDS returns the identity it used as a top-level field in the token response, using the fields already defined in the IMDS schema:

- `client_id`
- `object_id`
- `msi_res_id`

The contract that makes this safe: HIMDS sets the matching field only when it actually issued the token for that identity. It must never set the field while falling back to the system-assigned identity. Presence means "honored", not "I saw the parameter". An older agent that does not understand UAMI returns none of these fields.

The SDK reads these fields straight from the response by name. It does not read the swagger - the schema is the Azure Arc team's published contract.

## SDK behavior

For a user-assigned request (caller passed `client_id`, `object_id`, or `msi_res_id`):

- The matching field is present in the response -> the identity was honored -> return the token. The SDK also checks the value equals the requested identity as a sanity guard.
- The matching field is absent -> older agent that ignored the selector and returned SAMI -> fail with `user_assigned_managed_identity_not_supported`.

For a system-assigned request (no selector): no check; return the token as today.

The default is to reject: the SDK returns a user-assigned token only when the response positively confirms the identity. The SDK keeps client-side checks that do not depend on the agent, such as rejecting more than one selector.

## What we need from the Azure Arc team

1. Add `client_id` / `object_id` / `msi_res_id` to the HIMDS token response.
2. Set the matching field only when the requested identity was actually honored, never on a system-assigned fallback.
3. Echo the same selector the caller sent.

## Rollout across SDKs

The same block exists in the other MSAL SDKs and in the Azure Identity SDKs. Once the response contract is set, each SDK applies the same rule: validate the used-identity field and fail closed when it is missing. Roll out by current preview needs; languages do not have to ship together.

For each SDK:

1. Remove the hardcoded Azure Arc UAMI block.
2. Read the used-identity field from the token response and validate it against the request.
3. Fail closed when the field is missing.
4. Keep selector-specific cache partitioning (separate cache entries for SAMI and each UAMI).

## Tests

Positive:

- UAMI by `client_id`, `object_id`, and `msi_res_id` each return a token, and the response confirms the requested identity.
- SAMI request (no selector) returns a token.
- Separate cache entries for SAMI and each UAMI.

Negative:

- Response missing the used-identity field (older agent) -> request fails, no token returned.
- Response returns a different identity than requested -> request fails.
- More than one selector -> rejected by the SDK.

## Decision requested

- Replace the client-side Azure Arc UAMI block with response-based validation of the used identity.
- Azure Arc team adds `client_id` / `object_id` / `msi_res_id` to the token response and sets them only when the identity was honored.
- The SDK fails closed when the field is missing.
