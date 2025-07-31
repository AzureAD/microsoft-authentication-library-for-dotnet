# DoNotRunFederatedTestsAttribute

## Overview

The `DoNotRunFederatedTestsAttribute` is a custom MSTest attribute that allows you to conditionally skip federated authentication tests based on an environment variable. This is useful for CI/CD scenarios where federated tests may not be reliable or may require special setup.

## Usage

Apply the `[DoNotRunFederatedTests]` attribute to test methods or test classes that contain federated authentication tests:

```csharp
using Microsoft.Identity.Test.Common.Core.Helpers;

[TestClass]
public class FederatedAuthenticationTests
{
    [TestMethod]
    [DoNotRunFederatedTests]
    public async Task ROPC_ADFSv4Federated_Async()
    {
        // Test implementation for ADFS federated authentication
        var labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, true).ConfigureAwait(false);
        await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
    }
    
    [TestMethod]
    [DoNotRunFederatedTests]
    public async Task Interactive_AdfsV4_FederatedAsync()
    {
        // Test implementation for interactive ADFS authentication
        LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, true).ConfigureAwait(false);
        await RunTestForUserAsync(labResponse).ConfigureAwait(false);
    }
}
```

## Environment Variable

The attribute checks for the `MSAL_SKIP_FEDERATED_TESTS` environment variable:

- **Not set or empty**: Tests run normally
- **Set to any value**: Tests are skipped with `UnitTestOutcome.Inconclusive`

## CI/CD Integration

### Azure DevOps Pipelines

In your Azure DevOps pipeline YAML, set the environment variable for test tasks:

```yaml
- task: VSTest@2
  displayName: 'Run integration tests (.NET Core)'
  inputs:
    testSelector: 'testAssemblies'
    testAssemblyVer2: '**\Microsoft.Identity.Test.Integration.netcore\bin\**\Microsoft.Identity.Test.Integration.NetCore.dll'
    # ... other inputs
  env:
    MSAL_SKIP_FEDERATED_TESTS: '1'
```

### Local Development

For local development, you can set the environment variable to skip federated tests:

**Windows:**
```cmd
set MSAL_SKIP_FEDERATED_TESTS=1
dotnet test
```

**Linux/macOS:**
```bash
export MSAL_SKIP_FEDERATED_TESTS=1
dotnet test
```

## When to Use

Use this attribute for tests that:

- Require ADFS or other federated identity providers
- May be unreliable in CI environments
- Require special network configuration or VPN access
- Should be skipped in certain deployment scenarios

## Implementation Details

The attribute inherits from `TestMethodAttribute` and overrides the `Execute` method to check for the environment variable before proceeding with test execution. If the variable is set, it returns a test result with `UnitTestOutcome.Inconclusive` and an appropriate skip message.

## Migration from [Ignore]

When migrating from `[Ignore]` attributes for federated tests:

1. Remove the `[Ignore]` attribute
2. Add the `[DoNotRunFederatedTests]` attribute
3. Ensure the test is indeed a federated authentication test
4. Update CI/CD pipelines to set `MSAL_SKIP_FEDERATED_TESTS=1` when needed

This allows federated tests to run in environments where they're supported while being automatically skipped in environments where they're not.
