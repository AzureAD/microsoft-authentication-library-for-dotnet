# MSAL.NET Guidelines

## Overview

Microsoft Authentication Library (MSAL) for .NET is a highly scalable authentication library that enables applications to authenticate with the Microsoft identity platform. For confidential client scenarios (server-side applications), the library provides:

- Token acquisition for service-to-service calls
- Flexible token caching with distributed cache support
- Managed identity integration for Azure workloads
- On-behalf-of flow for service-to-service delegated access

Through its comprehensive feature set and proven reliability, MSAL.NET simplifies the implementation of secure authentication in server-side applications while maintaining optimal performance and security.

## Repository Structure

### Core Directories
- `/src/client/Microsoft.Identity.Client` - Core MSAL functionality
  - `ConfidentialClientApplication.cs` - Primary confidential client implementation
  - `ApiConfig/` - API configuration and builders
  - `Cache/` - Token cache implementations
  - `ManagedIdentity/` - Azure managed identity support 
  - `OAuth2/` - OAuth protocol implementations
  - `Internal/` - Internal components and utilities
- `/tests` - Unit tests and integration tests
- `/benchmark` - Performance benchmarking infrastructure
- `/tools` - Development and configuration tools

## Core Components

### Confidential Client Features
- Microsoft.Identity.Client - Main MSAL library with confidential client support
- Token cache implementations with extensible serialization
- Managed identity integration for Azure environments

### Authentication Components
- IConfidentialClientApplication - Primary interface for confidential clients
- Token cache providers and serialization extensibility
- Client credential builders and configurations
- Custom assertion providers (certificates, managed identity)

## Development Guidelines

### Core Development Principles
- Follow .editorconfig rules strictly
- Maintain backward compatibility due to widespread usage
- Implement proper error handling and retry logic
- Keep dependencies minimal and well-justified
- Document security considerations thoroughly
- Avoid using reflection in source code and tests when possible

### Authentication Best Practices
- Use certificate-based authentication over client secrets when possible
- Implement token caching for optimal performance
- Handle token expiration and refresh scenarios
- Configure appropriate token lifetimes
- Use managed identities in Azure environments when available

### Performance Requirements
- Implement distributed token caching for scale-out scenarios
- Optimize token acquisition patterns
- Use asynchronous APIs consistently
- Configure appropriate retry policies
- Benchmark token operations in high-throughput scenarios

### Security Guidelines
- Secure storage of client secrets and certificates
- Implement proper token validation
- Follow least-privilege principle for scopes
- Handle sensitive data appropriately
- Implement proper logging (avoiding sensitive data)

### Testing Requirements
- Maintain comprehensive test coverage
- Include integration tests with actual identity endpoints
- Test token cache implementations thoroughly
- Verify managed identity scenarios
- Include performance benchmarks for token operations

### Public API Changes
- The project uses Microsoft.CodeAnalysis.PublicApiAnalyzers
- For any public API changes:
  1. Update PublicAPI.Unshipped.txt in the package directory
  2. Include complete API signatures
  3. Consider backward compatibility impacts
  4. Document breaking changes clearly

Example format:
```diff
// Adding new API
+Microsoft.Identity.Client.ConfidentialClientApplication.AcquireTokenForClient() -> Task<AuthenticationResult>
+Microsoft.Identity.Client.IConfidentialClientApplication.GetAccounts() -> Task<IEnumerable<IAccount>>

// Removing API (requires careful consideration)
-Microsoft.Identity.Client.ConfidentialClientApplication.ObsoleteMethod() -> void
```

The analyzer enforces documentation of all public API changes in PublicAPI.Unshipped.txt and will fail the build if changes are not properly reflected.
