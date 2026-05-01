# Source code review rules

Applies to: src/**/*.cs

These rules apply when reviewing production source code in this repository.

## Namespace resolution — do NOT flag

- `Client.AppConfig.X`, `Client.Internal.X`, and similar short namespace references resolve via parent namespace `Microsoft.Identity`. Do not flag as unresolved.

## Coding standards

- Use `is null` / `is not null` instead of `== null` / `!= null`
- No reflection (`System.Reflection`, `Activator.CreateInstance`, `dynamic`). If reflection exists, it is a bug unless there is a comment justifying it.
- Static fields: `s_camelCase` (e.g., `s_knownHosts`)
- Private instance fields: `_camelCase`
- Ordinal string comparisons (`StringComparison.Ordinal` / `OrdinalIgnoreCase`) for protocol values, identifiers, hostnames, cache keys
- Validate inputs at method boundaries with specific exception types (`ArgumentNullException`, `ArgumentException`)
- Do not include secrets, tokens, or PII in exception messages or logs
- Use `nameof` instead of string literals for member names
- `ConfigureAwait(false)` is intentional in library code. Do not suggest removal.

## ConcurrentDictionary.GetOrAdd

`GetOrAdd(key, value)` eagerly evaluates the value argument. Flag any call where the second argument is not a delegate/lambda/method group:

- Bad: `pool.GetOrAdd(key, new ExpensiveObject());`
- Good: `pool.GetOrAdd(key, _ => new ExpensiveObject());`

## Public API changes

- Any public API additions or removals must be reflected in `PublicAPI.Unshipped.txt`
- XML doc comments are required on all public APIs
- Maintain backward compatibility — breaking changes require explicit justification

## MSAL-specific

- Use certificate-based authentication over client secrets when possible
- Use async APIs consistently
- Keep dependencies minimal and well-justified

## Scope

- Only comment on source code added or modified in the PR diff
- Do not comment on pre-existing code unless the PR directly introduces the issue
- Do not comment on style or formatting
- Focus on: bugs, security issues, logic errors, API contract violations
