# C# Development Guidelines

These guidelines define how to write and modify C# code in this repo.
They apply to both humans and coding agents.

> Note: Some rules are repeated intentionally (Quick rules + Detailed rules) to reduce “agent drift”
> and prevent hallucinated changes. The Detailed rules are the source of truth.

## Quick rules (read first)

- Use the C# language version configured by the repo/tooling. Do not “upgrade” language features by editing build files.
- Keep changes minimal and scoped to the request. Avoid drive-by refactors and style-only churn.
- Never change these unless explicitly asked to:
  - `global.json`
  - `package.json` / `package-lock.json`
  - `NuGet.config`
- **No reflection in product code** (`/src`). (See “Reflection” section.)
- Follow `.editorconfig` formatting.
- Static fields must be `s_camelCase`. (Match local style for other fields.)
- Prefer ordinal string comparisons for protocol/host/cache keys.
- Nullability: prefer non-nullable, validate at boundaries, use `is null` / `is not null`.
- Tests: MSTest SDK v3 + AAA comments + NSubstitute.

---

## General (Detailed rules)

### Language / tooling
- Prefer modern C# features **when supported by the repo’s configured language version**.
- Do not change `global.json` unless explicitly asked to.
- Do not change repo/tooling versions or build configuration unless explicitly asked.

### File / dependency hygiene
- Never change these unless explicitly asked to:
  - `global.json`
  - `package.json` / `package-lock.json`
  - `NuGet.config`
- Avoid adding/removing dependencies unless the task explicitly requires it.
- Do not commit build/test artifacts:
  - `bin/`, `obj/`, `TestResults/`, coverage outputs, temporary logs/dumps.

### “Don’t hallucinate” guardrails (for agents)
- Do not invent new repo conventions. If unsure, follow nearby code patterns.
- Do not introduce new abstractions/helpers unless there is an existing pattern in the repo.
- Prefer small, reviewable commits and changes.

---

## Formatting

- Apply code-formatting style defined in `.editorconfig`.
- Prefer file-scoped namespace declarations (when consistent with the file/project).
- Prefer single-line using directives (when consistent with the file/project).
- Insert a newline before the opening curly brace of any code block:
  - `if`, `for`, `while`, `foreach`, `using`, `try`, `catch`, `finally`, etc.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions when they improve clarity (do not refactor solely to use them).
- Use `nameof` instead of string literals when referring to member names.

---

## Naming & Style

- Follow naming conventions used in the same file/folder unless explicitly overridden here.

### Fields
- **Private static fields must be `s_camelCase`** (including `static readonly`).
  - Example: `private static readonly IDictionary<string, string> s_knownHosts = ...;`
- Private instance fields: follow local style (commonly `_camelCase`).

### Constants
- Use `PascalCase` for constants (unless the file uses a different established convention).

---

## Reflection & dynamic code (Product code)

**Do not use reflection in product code (`/src`)** unless explicitly requested or there is an established existing pattern in the same area that requires it.

Avoid introducing any of the following in product code:
- `System.Reflection` APIs (e.g., `GetMethod`, `GetProperty`, `Invoke`, `BindingFlags`, etc.)
- `Activator.CreateInstance(...)`
- `Assembly.Load(...)` / dynamic assembly loading
- `dynamic` dispatch used as a substitute for strong typing
- `System.Reflection.Emit` / runtime IL generation

If an exception is unavoidable (rare):
- Prefer a compile-time alternative first (generics, interfaces, explicit mappings).
- Add a short comment explaining why reflection is required and what alternatives were rejected.
- Keep reflection usage localized and test-covered.

Reflection is acceptable in:
- Tests, tooling, benchmarks, or dev apps (unless those areas have their own constraints).

---

## Nullable Reference Types

- Declare variables non-nullable by default, and validate `null` at entry points.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust C# null annotations and avoid redundant null checks when the type system guarantees non-null.

---

## Exceptions & error handling

- Validate inputs at method boundaries (fail fast).
- Throw the most specific exception type possible:
  - `ArgumentNullException`, `ArgumentException`, `InvalidOperationException`, etc.
- Do not swallow exceptions unless the behavior is expected and documented.
- Do not include secrets/tokens/PII in exception messages or logs.

---

## Strings, comparisons, and culture

- Prefer ordinal comparisons for protocol values, identifiers, hostnames, headers, and cache keys:
  - `StringComparison.Ordinal` / `StringComparison.OrdinalIgnoreCase`
  - `StringComparer.Ordinal` / `StringComparer.OrdinalIgnoreCase`
- Avoid culture-sensitive comparisons unless the string is user-facing (rare in libraries).

---

## Documentation

- Ensure that XML doc comments are created for any public APIs.
  - When applicable, include `<example>` and `<code>` documentation in the comments.
- Public API tracking rules exist in a separate guideline doc—follow that doc when public surface area changes.

---

## Testing

- We use MSTest SDK v3 for tests.
- Emit `// Arrange`, `// Act`, `// Assert` comments.
- Use NSubstitute for mocking in tests.
- Copy existing style in nearby files for test method names and capitalization.
- Prefer deterministic tests (avoid timing flakiness, environment dependence).

---

## Running tests

- To build and run tests in the repo, run `dotnet test`.
  - You need one solution open, or specify the solution explicitly.
- If the repo uses solution-specific or `msbuild` workflows for official validation, follow those.
