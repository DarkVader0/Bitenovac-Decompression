## General

* Make only high confidence suggestions when reviewing code changes.
* Always use the latest version C#.
* Never change global.json unless explicitly asked to.
* Never change nuget.config files unless explicitly asked to.
* Never change packages.lock.json files by hand; they are rewritten by restore.
* Keep model-specific parameters out of the Core library. Anything computable from the expanded
  profile and the equipment alone belongs in `Core/Calculations` and is shared by every model.

## Formatting

* Apply code-formatting style defined in `.editorconfig`.
* Prefer file-scoped namespace declarations and single-line using directives.
* Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`,
  `while`, `foreach`, `using`, `try`, etc.).
* Use pattern matching and switch expressions wherever possible.
* Use `nameof` instead of string literals when referring to member names.
* Ensure that XML doc comments are created for any public APIs, including `<exception>` for
  anything the member throws. Library projects fail the build without them.
* Warnings are errors. Do not suppress a warning to make a build pass.

### Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value
  cannot be null.

### Immutability

* Prefer immutable types. Validate arguments in the constructor and throw there.
* Copy any incoming collection so later mutation of the caller's list cannot be observed.
* Report expected planning outcomes through result types and flags, not exceptions. See
  `DecoPlan.IsValid` and `ReserveGasResult.AllSatisfied`.

### Testing

* We use xUnit SDK v3 for tests.
* Emit "Arrange", "Act" and "Assert" comments in every test, even when a section is empty.
* Name tests `Member_ShouldExpectation_WhenCondition`.
* Test classes are `sealed`, named `<TypeUnderTest>Tests`, and live flat in the test project.
* Compare doubles with `Assert.Equal(expected, actual, precision)` and a `private const int
  Precision` on the class.
* Use `TestFactory` for shared inputs. Do not mock; write a private fake in the test file.
* Derive expected values from the documented formula, never by pasting what the code returned.

## Running tests

* To build and run tests, run `dotnet test` against a test project from the repository root, e.g.
  `dotnet test .\tests\libraries\Bitenovac.DecompressionAlgorithms.Core.Unit.Tests\Bitenovac.DecompressionAlgorithms.Core.Unit.Tests.csproj`.
* Tests run on Microsoft.Testing.Platform, not VSTest. "No test projects were found" usually means
  the project path did not resolve.
