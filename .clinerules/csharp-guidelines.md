# C# Development Guidelines

These guidelines define how to write and modify C# code in this repo. They apply to both humans and coding agents.

## General

- Use the C# language version and SDK **as configured by the repo**.
  - Do **not** change `global.json` unless explicitly asked to.
- Keep changes **minimal and scoped** to the request.
  - Avoid drive-by refactors, renames, and reformatting unrelated code.
- Never change these unless explicitly asked to:
  - `global.json`
  - `package.json` / `package-lock.json`
  - `NuGet.config`
- Do not commit build/test artifacts:
  - `bin/`, `obj/`, `TestResults/`, coverage outputs, temporary logs/dumps, etc.
- Do not suppress analyzers/warnings (`#pragma warning disable`) unless:
  - There is no viable alternative, AND
  - The suppression is narrow, documented, and consistent with nearby code.

## Formatting

- Follow `.editorconfig` exactly.
- Prefer file-scoped namespaces where the file/project uses them.
- Prefer single-line `using` directives.
- Avoid formatting churn:
  - Only reformat lines you touch unless explicitly asked to run a formatter on the file.
- Braces:
  - Insert a newline before the opening `{` for any code block (`if`, `for`, `while`, `foreach`, `using`, `try`, `catch`, etc.).
- Returns:
  - Ensure the final return statement of a method is on its own line.
- Prefer clarity over cleverness:
  - Use pattern matching / switch expressions when they reduce complexity and improve readability.

## Naming & Style

- Follow existing naming patterns in the file and folder.
- `nameof(...)`:
  - Use `nameof` instead of string literals when referring to member/parameter names.
- Constants:
  - Use `PascalCase` for const fields.
- Fields:
  - **Private static fields must be `s_camelCase`** (including `static readonly`).
  - Private instance fields should match local convention (commonly `_camelCase`).
- Avoid introducing new abbreviations unless they are established MSAL terms.

## Nullability & Guard Clauses

- Default to **non-nullable** types.
- Validate at entry points:
  - Check for `null` at public or boundary methods (inputs, external data, deserialization, etc.).
- Use pattern checks:
  - Always use `is null` / `is not null`, not `== null` / `!= null`.
- Trust nullability annotations:
  - Don’t add redundant null checks when the type system guarantees non-null.
- Prefer fail-fast:
  - Validate early and return/throw before doing non-trivial work.

## Exceptions & Error Handling

- Throw the most specific exception you can:
  - `ArgumentNullException`, `ArgumentException`, `InvalidOperationException`, etc.
- Don’t swallow exceptions unless:
  - You can recover safely, AND
  - The behavior is expected and documented.
- Exception messages:
  - Do not include secrets, tokens, credentials, or PII.
  - Avoid logging full URLs if they can contain sensitive query parameters.

## Strings, Comparisons, and Culture

- Use **ordinal** comparisons for protocol values, identifiers, hostnames, headers, cache keys:
  - `StringComparison.Ordinal` / `StringComparison.OrdinalIgnoreCase`
  - `StringComparer.Ordinal` / `StringComparer.OrdinalIgnoreCase`
- Use culture-aware comparisons only for genuinely user-facing text (rare in libraries).

## Collections & Performance

- Prefer `TryGetValue` for dictionary lookups.
- Prefer `IReadOnly*` where mutation isn’t required.
- Avoid LINQ in hot paths if nearby code avoids it.
- Avoid unnecessary allocations in frequently-used code paths (especially authentication/token acquisition).

## Async & Concurrency

- Do not block on async (`.Result`, `.Wait()`).
- Prefer async all the way through when interacting with async APIs.
- Cancellation:
  - Accept and pass through `CancellationToken` when patterns exist nearby.
- ConfigureAwait:
  - Follow the repo’s local convention (don’t introduce inconsistency).

## Documentation

- Public APIs require XML doc comments.
  - When useful, include `<example>` and `<code>`.
- Comments should explain **why**, not **what**.
- Public API lifecycle/tracking is documented separately:
  - If you touched public surface area, follow the repo’s **Public API guidelines doc**.

## Testing

- Use MSTest SDK v3.
- Use AAA comments:
  - `// Arrange`, `// Act`, `// Assert`
- Use NSubstitute for mocking.
- Match local conventions for:
  - Test naming, casing, and structure.
- Keep tests deterministic:
  - Avoid timing flakiness and environment coupling.
- If you fix a bug, add a regression test that fails before the fix and passes after.

## Running Tests

- Use the repo’s recommended build/test approach.
- As a general quick check, `dotnet test` is acceptable when it works for the project/solution you changed.
- If the repo uses solution-specific or `msbuild` workflows, follow those for “official” validation.
