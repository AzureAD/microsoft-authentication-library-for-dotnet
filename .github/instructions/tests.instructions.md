---
applyTo: "tests/**/*.cs"
---

# Test file review rules

These rules apply when reviewing test files in this repository.

## Test framework patterns — do NOT flag

- `[RunOn]` inherits from `TestMethodAttribute` (see `tests/Microsoft.Identity.Test.Integration.netcore/Infrastructure/TargetFramework.cs` line 15). Tests decorated with `[RunOn]` WILL be discovered by MSTest. Do not flag as missing `[TestMethod]`.
- `Assert.IsTrue(bool?)` is a valid MSTest overload. Do not flag nullable bool arguments as type mismatches.
- `Assert.DoesNotContain(substring, value)` — in MSTest v4, the first argument is the substring and the second is the value to search. Do not suggest swapping arguments.
- `Assert.HasCount(expected, collection)` — valid MSTest v4 assertion. Do not suggest `Assert.AreEqual` for count checks.

## Test conventions

- Use MSTest SDK v4 with NSubstitute for mocking
- Use `// Arrange`, `// Act`, `// Assert` comments
- Prefer deterministic tests: avoid `Thread.Sleep`, timing dependencies, or environment-specific behavior
- Copy existing style in nearby files for test method names

## Scope

- Only comment on test code that is added or modified in the PR diff
- Do not comment on pre-existing test patterns or style
- Do not re-post comments already made on earlier commits

## Test strategy

- Integration tests are slow and should only be used for mainline sceanrios.
- Prefer unit tests that mock the HTTP layer. A good unit test uses the public API and uses minimal mocking to verify behavior. Ideally you only mock the HTTP layer, but sometimes it isn't possible, for example for MSI sceanrios env variables need to be mocked etc.
