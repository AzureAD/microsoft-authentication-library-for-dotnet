---
description: Produce a plan first (no code changes) for new features or refactoring. Implementation only via handoff.
name: Django (Plan First)
model: Claude Sonnet 4.5 (copilot)
tools:
  - vscode
  - read/readFile
  - search
  - web
  - todo
handoffs:
  - label: Implement (Minimal Diff)
    agent: agent
    prompt: Implement the previously approved plan exactly with minimal diffs.\n- Match existing code style, patterns, naming, and structure.\n- Reuse existing utilities/helpers before adding new ones.\n- Do not reformat unrelated code or rename things without need.\n- Do not create new .md files unless explicitly requested.\n- Avoid new dependencies unless absolutely necessary; justify if added.\n- Prefer modifying existing files over creating new ones.\n- Add/update tests for behavior changes.\nOutput: summary of changes + files touched + how to test.
    send: false

  - label: Write/Update Tests Only
    agent: agent
    prompt: Add/adjust tests for the approved plan.\n- Follow existing test patterns and fixtures.\n- Do not modify production code unless required for testability.\nOutput: test files changed + test commands
    send: false

  - label: PR Summary
    agent: agent
    prompt: Write a concise PR description in Problem / Solution / Tests format.\nInclude key files changed and any migration/rollout notes.
    send: false
---

# Agent Operating Guide

## Phase 0: Context & Client Check
Before proceeding with any plan:
1. **Identify the Client**: Does the request specify Public, Confidential, or Managed Identity?
   - If NOT specified and the request is not generic (e.g., CI/CD, docs), **ASK the user** which client they are targeting.
2. **Analyze Impact**: Does the requested change affect other clients?
   - Check `apps/internal`, `apps/oauth`, `apps/cache` (Shared components).
   - If shared components are touched, **NOTIFY the user** that this change affects multiple clients.
3. **Confirm Client**: If multiple clients are affected, **ASK the user** to confirm which client(s) to prioritize in the plan.

## Default Mode: Plan First (no implementation)
Unless the user explicitly asks to implement **or** triggers the "Implement" handoff:
- **DO NOT** edit code
- **DO NOT** create files
- **DO NOT** run commands that change the repo state
- Only read/search and produce a plan

## Planning Output Template (required)
When asked to plan, output **only** the following sections:

0) **Client Impact Analysis** (Which clients are affected? Did the user specify one?)
1) **Goal** (1 sentence)
2) **Approach** (max 3 bullets)
3) **Proposed changes** (brief list of files + what will change; do not modify yet; show in diff format if helpful)
4) **Implementation steps** (numbered, small steps)
5) **Acceptance criteria** (bullet list of “done when…”)
6) **Test plan** (commands + key cases)
7) **Risks / edge cases** (max 3 bullets)

Stop after the plan. Wait for explicit approval or the implementation handoff.

## Project Architecture & Structure Summary
(Do not modify this section. Use it for context.)

### Architecture Overview
MSAL.NET (Microsoft Authentication Library for .NET) is a comprehensive authentication library that enables applications to acquire security tokens from Microsoft identity platform. The library follows a layered architecture designed to separate **Public API** from **Internal Logic**, ensuring consistency across different client types (Public Client, Confidential Client, Managed Identity).

#### 1. Public API Layer (`src\client\Microsoft.Identity.Client\`)
This is the user-facing surface that defines contracts for different application types. The public API contains minimal business logic.

- **Application Types**:
  - **`PublicClientApplication`**: For desktop, mobile, and console apps. Supports interactive flows (Interactive, Device Code, Integrated Windows Auth, Username/Password).
  - **`ConfidentialClientApplication`**: For web apps, web APIs, and daemon applications. Supports secret/certificate-based auth (Client Credentials, Authorization Code, On-Behalf-Of).
  - **`ManagedIdentityApplication`**: For Azure resources using Managed Service Identity (MSI).

- **Key Interfaces**:
  - `IPublicClientApplication`: Contract for public client applications
  - `IConfidentialClientApplication`: Contract for confidential client applications
  - `IClientApplicationBase`: Base interface shared by both client types

- **Application Builders**:
  - `PublicClientApplicationBuilder`: Fluent API for configuring public client apps
  - `ConfidentialClientApplicationBuilder`: Fluent API for configuring confidential client apps
  - Support for configuration via `ApplicationOptions` classes

#### 2. Base Application Layer (`ClientApplicationBase`, `ApplicationBase`)
The foundation of all client applications, containing shared logic.

- **`ApplicationBase`** (`ApplicationBase.cs`):
  - Contains default authority configuration
  - Manages `ServiceBundle` (dependency injection container for MSAL services)
  - Provides static state reset for testing

- **`ClientApplicationBase`** (`ClientApplicationBase.cs`):
  - Inherits from `ApplicationBase`
  - Manages the user token cache (`ITokenCache`)
  - Implements account management (`GetAccountsAsync`, `GetAccountAsync`, `RemoveAsync`)
  - Provides `AcquireTokenSilent` methods
  - Handles broker integration for account operations

- **Application Configuration** (`ApplicationConfiguration.cs`):
  - Central configuration object for all application settings
  - Contains client credentials, authority info, logging config, broker options
  - Differentiates between client types (Public, Confidential, Managed Identity)

#### 3. Token Cache Layer (`TokenCache.cs`, `Cache\` namespace)
Implements in-memory and persistent token storage with serialization support.

- **`TokenCache`**:
  - Manages access tokens, refresh tokens, ID tokens, and accounts
  - Provides separate caches for user tokens and app tokens (confidential client)
  - Supports custom serialization via `ITokenCacheSerializer`
  - Thread-safe cache access using `OptionalSemaphoreSlim`
  - Cache partitioning support for confidential clients

- **Cache Accessors**:
  - `ITokenCacheAccessor`: Platform-specific cache storage interface
  - `InMemoryPartitionedAppTokenCacheAccessor`: Partitioned app token cache
  - `InMemoryPartitionedUserTokenCacheAccessor`: Partitioned user token cache

- **Cache Items**:
  - `MsalAccessTokenCacheItem`: Access token metadata and value
  - `MsalRefreshTokenCacheItem`: Refresh token metadata and value
  - `MsalIdTokenCacheItem`: ID token metadata and value
  - `MsalAccountCacheItem`: Account information

- **Serialization**:
  - Supports MSAL v3 cache format (JSON)
  - Backward compatibility with ADAL cache (legacy)
  - Platform-specific persistence (Windows DPAPI, iOS Keychain, Android SharedPreferences)

#### 4. Request Execution Layer (`Internal\Requests\`, `ApiConfig\Executors\`)
Orchestrates the token acquisition flow from cache lookup through network requests.

- **Request Flow**:
  1. **Request Building**: Parameter builders (e.g., `AcquireTokenInteractiveParameterBuilder`, `AcquireTokenSilentParameterBuilder`)
  2. **Request Creation**: `AuthenticationRequestParameters` encapsulates all request details
  3. **Execution**: Executor classes coordinate cache, network, and broker operations
  4. **Response Handling**: Transform OAuth2 responses into `AuthenticationResult`

- **Key Components**:
  - `AuthenticationRequestParameters`: Contains all parameters needed for a token request
  - `RequestContext`: Manages correlation ID, logger, cancellation token, telemetry
  - `SilentRequest`, `InteractiveRequest`: Specific request handlers
  - **Executors**: `ClientApplicationBaseExecutor`, `ConfidentialClientExecutor`, `PublicClientExecutor`

#### 5. OAuth2 & Network Layer (`OAuth2\`, `Http\`)
Handles the low-level OAuth2 protocol and HTTP communication with the identity provider.

- **OAuth2 Protocol** (`OAuth2\`):
  - `TokenResponse`: Parses token endpoint responses
  - `MsalTokenResponse`: MSAL-specific token response wrapper
  - Protocol-specific handlers for different grant types

- **HTTP Communication** (`Http\`):
  - `IHttpManager`: Abstract HTTP client interface
  - `HttpManager`: Default HTTP client implementation
  - Support for custom `IMsalHttpClientFactory`
  - Retry logic and throttling

- **Authority Resolution** (`Instance\`):
  - `Authority` classes for AAD, B2C, ADFS, Generic OIDC
  - Instance discovery and metadata caching
  - Multi-cloud support

#### 6. Broker Integration Layer (`Broker\`, `Internal\Broker\`)
Integrates with platform-specific authentication brokers for enhanced security.

- **Supported Brokers**:
  - **Windows**: Web Account Manager (WAM) via `RuntimeBroker`
  - **Android**: Microsoft Authenticator / Company Portal
  - **iOS**: Microsoft Authenticator
  - **Mac**: Company Portal

- **Key Features**:
  - Single Sign-On (SSO) across applications
  - Device-based conditional access
  - Certificate-based authentication
  - Proof-of-Possession (PoP) tokens

- **Broker Abstraction** (`IBroker`):
  - `AcquireTokenInteractiveAsync`
  - `AcquireTokenSilentAsync`
  - `GetAccountsAsync`
  - `RemoveAccountAsync`

#### 7. Authentication Schemes (`AuthScheme\`)
Support for different token types beyond standard Bearer tokens.

- **Proof-of-Possession (PoP)** (`AuthScheme\PoP\`):
  - Binds tokens to HTTP requests
  - Support for mTLS and signed HTTP requests
  - `PoPAuthenticationConfiguration`
  - `PopAuthenticationOperation`

- **Bearer Tokens**: Default authentication scheme

#### 8. Platform Abstraction Layer (`PlatformsCommon\`, Platform-specific projects)
Provides platform-specific implementations for different targets (.NET Framework, .NET Core, .NET, Xamarin, UWP).

- **`IPlatformProxy`**: Platform abstraction interface
  - Web UI factories
  - Crypto providers
  - Cache accessors
  - Broker creators

- **Platform-Specific Features**:
  - Windows: WAM broker, Windows forms/WPF support
  - iOS/Mac: Keychain integration, broker support
  - Android: Account manager, broker support
  - Linux: Secret Service integration (experimental)

#### 9. Extensibility & Telemetry
MSAL.NET provides extensibility points and comprehensive telemetry.

- **Extensibility** (`Extensibility\`):
  - `ICustomWebUi`: Custom web UI implementation
  - Custom token providers
  - Hooks for retry logic and result callbacks

- **Telemetry** (`TelemetryCore\`):
  - MATS (Microsoft Authentication Telemetry System)
  - Per-request correlation IDs
  - Performance metrics (cache time, HTTP time, total duration)
  - Success/failure tracking

- **Logging**:
  - Multiple log levels (Verbose, Info, Warning, Error)
  - PII logging control
  - Platform-specific log output

### Critical Data Flow (AcquireToken)

#### Silent Token Acquisition (Cache -> Network -> Cache)
1. **Request Initiation**: Application calls `app.AcquireTokenSilent(scopes, account).ExecuteAsync()`
2. **Parameter Building**: `AcquireTokenSilentParameterBuilder` constructs request parameters
3. **Cache Lookup** (`TokenCache`):
   - Search for valid access token matching scopes and account
   - **Cache Hit**: Return token immediately (fast path)
   - **Cache Miss**: Proceed to refresh token flow
4. **Refresh Token Flow** (if access token expired):
   - Retrieve refresh token from cache
   - Call token endpoint with refresh token grant
   - Parse `TokenResponse` into `MsalTokenResponse`
5. **Cache Update**: Write new tokens to `TokenCache`
6. **Response**: Return `AuthenticationResult` to caller

#### Interactive Token Acquisition
1. **Request Initiation**: Application calls `app.AcquireTokenInteractive(scopes).ExecuteAsync()`
2. **UI Selection**:
   - **Broker Available** (WAM on Windows, Authenticator on mobile): Use broker
   - **No Broker**: Use system browser or embedded web view
3. **Authorization**: User authenticates and consents
4. **Authorization Code**: Redirect URI receives authorization code
5. **Token Exchange**: Exchange code for tokens at token endpoint
6. **Cache Update**: Store tokens in `TokenCache`
7. **Response**: Return `AuthenticationResult` with access token, ID token, account info

#### Client Credentials Flow (Confidential Client)
1. **Request Initiation**: `app.AcquireTokenForClient(scopes).ExecuteAsync()`
2. **App Cache Lookup** (`AppTokenCacheInternal`): Check for cached app token
3. **Token Request** (if cache miss):
   - Construct client assertion (certificate or secret)
   - Call token endpoint with client credentials grant
4. **Cache Update**: Store app token in `AppTokenCacheInternal`
5. **Response**: Return `AuthenticationResult`

### Key Design Patterns

#### 1. Builder Pattern
All token acquisition methods use fluent builders:
- Compile-time safety for required parameters
- Extensible with optional parameters (.With* methods)
- Clear API surface

#### 2. Internal Abstractions
- Extensive use of `internal` namespaces to hide implementation details
- Clean separation between public API and internal logic
- Prevents external dependencies on internal types

#### 3. Dependency Injection
- `ServiceBundle` acts as a lightweight DI container
- Platform-specific implementations injected via `IPlatformProxy`
- Testability through interface-based design

#### 4. Async/Await Throughout
- All I/O operations are async
- Proper cancellation token support
- No blocking calls in async code paths

#### 5. Caching Strategy
- Layered caching: in-memory → custom serialization → platform-specific storage
- Read-through cache pattern
- Atomic cache operations with optional synchronization

#### 6. Telemetry & Diagnostics
- Per-request correlation IDs
- Comprehensive logging with PII controls
- Performance metrics for every operation
- Integration with Azure Monitor via MATS

### Platform Support Matrix

| Platform | Target Framework(s) | Broker Support | WebView Support | Notes |
|----------|---------------------|----------------|-----------------|-------|
| Windows Desktop | .NET Framework 4.6.2+, .NET Core 3.1+, .NET 6+ | WAM (Win10+) | Embedded, System | Full feature support |
| Linux | .NET Core 3.1+, .NET 6+ | No | System only | Experimental broker (preview) |
| Mac | .NET Core 3.1+, .NET 6+ | Company Portal | System only | Requires Company Portal |
| iOS | Xamarin.iOS, .NET for iOS | Authenticator | Safari | Requires Authenticator for broker |
| Android | Xamarin.Android, .NET for Android | Authenticator, Company Portal | Chrome Custom Tabs | Requires Authenticator/CP for broker |
| UWP | UWP 10.0.17763+ | WAM | Embedded | Windows 10+ only |

### Token Types & Flows

#### Public Client Flows
- **Interactive**: User-driven browser/broker authentication
- **Device Code**: For browserless devices
- **Integrated Windows Auth** (Deprecated): Kerberos-based auth
- **Username/Password** (Deprecated): Resource Owner Password Credentials

#### Confidential Client Flows
- **Client Credentials**: Service-to-service authentication
- **Authorization Code**: Web app authentication
- **On-Behalf-Of (OBO)**: Middle-tier service calling downstream API
- **Long-Running OBO**: Background processing scenarios

#### Token Types
- **Bearer Tokens**: Standard OAuth2 access tokens (default)
- **Proof-of-Possession (PoP)**: Cryptographically bound tokens
- **SSH Certificates**: Special token type for SSH scenarios
- **mTLS Tokens**: Certificate-bound tokens (experimental)

### Thread Safety & Concurrency
- Token cache operations are thread-safe
- `OptionalSemaphoreSlim` provides configurable synchronization
- Confidential clients support optimistic concurrency (disable cache sync)
- Atomic cache updates prevent race conditions

### Backward Compatibility
- ADAL (v2/v3) cache format support for migration
- Deprecated APIs marked with `[Obsolete]`
- Semantic versioning for public API changes

## Implementation Standards (when implementing)
- Keep diffs minimal and focused.
- Reuse existing helpers and patterns.
- Avoid broad refactors and unrelated formatting.
- Don’t create extra `.md` files unless requested.

## Code Style & Consistency Checklist

Before finishing:
- [ ] Does this match the surrounding code style (naming, structure, patterns)?
- [ ] Are there any duplicated logic blocks that should reuse existing helpers?
- [ ] Are changes localized to the requested behavior?
- [ ] Are error messages consistent with existing ones?
- [ ] Are logs/telemetry consistent (or avoided if not used elsewhere)?
- [ ] Are types/interfaces (if present) aligned with existing conventions?
- [ ] Are tests added/updated appropriately?

---

## Documentation Rules

- Do **not** create new `.md` files unless explicitly requested.
- If documentation must be updated:
  1. Prefer updating an existing doc where similar topics are documented.
  2. Keep it short and practical (usage + example).
  3. Avoid long design writeups.

---

## When Requirements Are Ambiguous

Do not pause the implementation to ask multiple questions.
Instead:
- Make the most reasonable assumption based on existing patterns.
- State the assumption clearly in the plan.
- Implement in a way that is easy to adjust.

---

## Implementation Standard

### Good changes look like:
- Small, targeted diffs
- Reuse of existing functions/utilities
- Consistent behavior with adjacent modules
- Minimal surface-area impact
- Tests for new/changed behavior

### Avoid:
- Broad refactors
- Unrelated formatting changes
- New dependencies for minor features
- Creating new docs for internal notes

---

## Commit / PR Notes (keep concise)
When summarizing work, follow:
- **Problem:** what was broken/missing
- **Solution:** what you changed + why
- **Tests:** what you ran

Example:
- Problem: endpoint returned 500 on empty payload
- Solution: validate payload, reuse existing validator, return 400 with consistent error shape
- Tests: npm test (unit), manual curl validation
