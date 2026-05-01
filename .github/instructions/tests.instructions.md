# Test file review rules

Applies to: tests/**/*.cs

These rules apply when reviewing test files in this repository.

## Test framework patterns — do NOT flag

- `[RunOn]` inherits from `TestMethodAttribute` (see `tests/Microsoft.Identity.Test.Integration.netcore/Infrastructure/TargetFramework.cs` line 15). Tests decorated with `[RunOn]` WILL be discovered by MSTest. Do not flag as missing `[TestMethod]`.
- `Assert.IsTrue(bool?)` is a valid MSTest overload. Do not flag nullable bool arguments as type mismatches.
- `Assert.DoesNotContain(substring, value)` — in MSTest v4, the first argument is the substring and the second is the value to search. Do not suggest swapping arguments.
- `Assert.HasCount(expected, collection)` — valid MSTest v4 assertion. Do not suggest `Assert.AreEqual` for count checks.

## Test conventions

- Use MSTest SDK v3 with NSubstitute for mocking
- Use `// Arrange`, `// Act`, `// Assert` comments
- Prefer deterministic tests: avoid `Thread.Sleep`, timing dependencies, or environment-specific behavior
- Copy existing style in nearby files for test method names

## Scope

- Only comment on test code that is added or modified in the PR diff
- Do not comment on pre-existing test patterns or style
- Do not re-post comments already made on earlier commits
