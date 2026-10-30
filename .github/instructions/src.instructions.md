---
applyTo: "src/**/*.cs"
---

# Source code review rules

These rules apply when reviewing production source code in this repository.

## Multi-targetting support

To support multi-targetting, 2 approaches are used:

- #if directives to conditionally compile code for different target frameworks
- conditional compilaton of entire files in src/client/Microsoft.Identity.Client/Platforms

Since most development happens in .NET and .NET FWK, which have similar capabilities, we generally prefer #if directives, as they are easier to understand. 

### Mobile support

Some interfaces are implemented differently on mobile platforms. The regular build does not build the entire solution, it skips the mobile targets. 

## Namespace resolution — do NOT flag

- `Client.AppConfig.X`, `Client.Internal.X`, and similar short namespace references resolve via parent namespace `Microsoft.Identity`. Do not flag as unresolved.

## ConcurrentDictionary.GetOrAdd

`GetOrAdd(key, value)` eagerly evaluates the value argument. Flag any call where the second argument is not a delegate/lambda/method group:

- Bad: `pool.GetOrAdd(key, new ExpensiveObject());`
- Good: `pool.GetOrAdd(key, _ => new ExpensiveObject());`

## `ConfigureAwait(false)`

`ConfigureAwait(false)` is intentional in library code. Do not suggest removal.

## Public API changes

- Any public API additions or removals must be reflected in `PublicAPI.Unshipped.txt`
- XML doc comments are required on all public APIs
- Maintain backward compatibility — breaking changes require explicit justification

## MSAL-specific

- Keep dependencies minimal and well-justified

## Scope

- Only comment on source code added or modified in the PR diff
- Do not comment on pre-existing code unless the PR directly introduces the issue
- Do not comment on style or formatting — coding standards are enforced via .editorconfig
- Focus on: bugs, security issues, logic errors, API contract violations
