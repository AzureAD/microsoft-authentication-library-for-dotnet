# Microsoft.Identity.Client.Broker.OneAuth

This package contains an experimental implementation of MSAL.NET broker functionality using OneAuth C# projections instead of the traditional NativeInterop/MSALRuntime approach.

## 🚀 Current Status

✅ **Successfully Implemented**
- Package structure created and building for both .NET Standard 2.0 and .NET Framework 4.6.2
- Adapter pattern implementation (`OneAuthAdapter.cs`, `OneAuthParameterMappers.cs`)
- Placeholder types enable local development without OneAuth NuGet feed authentication
- Complete integration with MSAL.NET solution
- All major compilation issues resolved

🔄 **Next Steps**
- Set up Azure DevOps authentication for OneAuth NuGet feed
- Replace placeholder types (`OneAuthPlaceholder.cs`) with actual OneAuth package
- Complete testing with real OneAuth implementation
- Fine-tune parameter mapping and error handling

## Setup Requirements

### 1. Authentication for OneAuth Feed

The OneAuth package is hosted on an internal Azure DevOps feed that requires authentication:

```bash
# Add the OneAuth feed with authentication
dotnet nuget add source https://office.visualstudio.com/OneAuth/_packaging/OneAuth/nuget/v3/index.json \
  --name OneAuth \
  --username [your-email] \
  --password [PAT-token] \
  --store-password-in-clear-text

# Alternative: Use Azure Artifacts Credential Provider
# Install: https://github.com/microsoft/artifacts-credprovider
```

### 2. Package Source Configuration

The project is configured with package source mapping in `NuGet.Config`:

```xml
<packageSourceMapping>
  <packageSource key="OneAuth">
    <package pattern="Microsoft.OneAuth*" />
  </packageSource>
</packageSourceMapping>
```

## Purpose

This package is designed to:

1. **Parallel Development**: Allow development and testing of OneAuth integration alongside the existing broker implementation
2. **Risk Mitigation**: Provide a separate package that doesn't affect existing functionality
3. **Feature Flag Support**: Enable switching between implementations using configuration or feature flags
4. **Gradual Migration**: Support incremental adoption of OneAuth-based broker functionality

## Package Structure

```
Microsoft.Identity.Client.Broker.OneAuth/
├── OneAuth.cs                      # OneAuth C# projection wrapper
├── OneAuthAdapter.cs               # Adapter layer for MSAL.NET compatibility
├── OneAuthParameterMappers.cs      # Parameter mapping utilities
├── OneAuthRuntimeBroker.cs         # OneAuth-based broker implementation
├── OneAuthInteropTypes.cs          # Placeholder types (replace with actual OneAuth package)
└── PublicAPI/                      # API analyzer files
```

## Key Differences from Original Broker Package

### Initialization
- **Original**: Lazy initialization of `NativeInterop.Core`
- **OneAuth**: Explicit `Startup()` call with configuration objects

### Parameter Structure
- **Original**: Single `AuthParameters` object
- **OneAuth**: Split into specialized parameters (`AuthParameters`, `SignInBehaviorParameters`, `TelemetryParameters`)

### Context Handling
- **Original**: Uses `IntPtr parentHwnd` directly
- **OneAuth**: Requires `UxContext` object

### Cancellation Support
- **Original**: Native `CancellationToken` support
- **OneAuth**: Wrapper pattern using `CancelAllTasks()`

## Usage

This package is intended for:

1. **Testing OneAuth Integration**: Validate OneAuth functionality in isolated environment
2. **Feature Development**: Implement new OneAuth-specific features
3. **Migration Planning**: Compare behavior between implementations

## Dependencies

Based on OneAuth CMake configuration:
- `Microsoft.Bcl.AsyncInterfaces` (9.0.1) - For `IAsyncEnumerable` support in .NET Standard 2.0
- `System.Drawing.Common` (9.0.1) - For profile image support
- `System.Threading.Channels` (9.0.1) - For async enumerable implementation

## Configuration Options

The package supports behavior controls as defined in the migration design:

```csharp
public class OneAuthBehaviorOptions
{
    public bool UseOfficeHrd { get; set; } = true;              // Use Office HRD vs Accounts Control
    public bool SuppressSilentDiscovery { get; set; } = true;   // Suppress background discovery
    public bool LimitToIdTokenClaims { get; set; } = true;      // Limit account claims to ID token
    public bool SkipGraphTokenAcquisition { get; set; } = true; // Skip MS Graph integration
}
```

## Development Status

**🚧 Experimental - Not for Production Use**

This package is currently in development and requires:

1. **Actual OneAuth Package**: Replace placeholder types with real OneAuth NuGet package
2. **Parameter Mapping**: Complete implementation of parameter mappers
3. **Result Conversion**: Implement OneAuth result to MSAL token response conversion
4. **Error Handling**: Map OneAuth errors to MSAL exception types
5. **Testing**: Comprehensive testing across all scenarios

## Future Plans

- Replace placeholder types with actual OneAuth package reference
- Implement complete parameter mapping
- Add comprehensive error handling
- Support feature flag switching between implementations
- Eventually replace the original broker package

## Related Documentation

- [MSAL.NET OneAuth Integration Design Document](link-to-design-doc)
- [OneAuth C-ABI Design Document](https://office.visualstudio.com/OneAuth/_git/OneAuth?path=/docs/design/c_abi.md&_a=preview)
- [MSALRuntime Integration Documentation](existing-docs-link)
