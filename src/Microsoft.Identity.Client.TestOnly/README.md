# Microsoft.Identity.Client.TestOnly

Reusable test infrastructure for [MSAL.NET](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) consumers.

## Overview

This package provides mock HTTP factories, message handlers, and logging helpers that enable deterministic unit and integration testing of code that uses MSAL.NET for authentication.

## Getting Started

### Basic HTTP mocking

```csharp
var httpFactory = new MockHttpClientFactory();

// Queue a mock response
httpFactory.AddMockHandler(new MockHttpMessageHandler
{
    ExpectedUrl = "https://login.microsoftonline.com/tenant/oauth2/v2.0/token",
    ExpectedMethod = HttpMethod.Post,
    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"access_token\":\"fake-token\",\"token_type\":\"Bearer\",\"expires_in\":3600}")
    }
});

// Pass to MSAL builder
var app = ConfidentialClientApplicationBuilder
    .Create("client-id")
    .WithClientSecret("secret")
    .WithHttpClientFactory(httpFactory)
    .Build();
```

### mTLS scenarios

```csharp
var httpFactory = new MockHttpClientFactory();
httpFactory.AddMockHandler(new MockHttpMessageHandler
{
    ExpectedMethod = HttpMethod.Post,
    ExpectedMtlsBindingCertificate = myCert,
    ResponseMessage = MockHttpMessageHandlerHelpers.CreateSuccessTokenResponse()
});

var app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .WithHttpClientFactory(httpFactory)  // also IMsalMtlsHttpClientFactory
    .Build();
```

### Test logging

```csharp
var logger = new TestIdentityLogger(EventLogLevel.Verbose);
var app = PublicClientApplicationBuilder
    .Create("client-id")
    .WithLogging(logger)
    .Build();
// Inspect: logger.StringBuilder.ToString()
```

## Target Frameworks

- `netstandard2.0`
- `net48`
- `net8.0`

## Notes

- This package is intentionally coupled to MSAL.NET and tracks its version closely.
- Package versions follow the pattern `<MSAL version>-test.<N>` (e.g., `4.82.0-test.1`).
