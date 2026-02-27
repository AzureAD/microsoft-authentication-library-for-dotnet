# Error Handling Patterns

## Overview
Proper error handling ensures robust applications and helps with debugging.

## Common Exceptions

### MsalServiceException
```csharp
try
{
    var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
}
catch (MsalServiceException ex)
{
    // Service returned an error
    var statusCode = ex.StatusCode;  // HTTP status code
    var error = ex.ErrorCode;        // AAD error code
    var message = ex.Message;        // Error description
    
    // Log and handle based on error code
    logger.LogError($"MSAL Service Error: {error} - {message}");
}
```

### MsalClientException
```csharp
catch (MsalClientException ex)
{
    // Client-side error (invalid parameters, certificate issues)
    logger.LogError($"MSAL Client Error: {ex.ErrorCode} - {ex.Message}");
}
```

### MsalUiRequiredException
```csharp
catch (MsalUiRequiredException ex)
{
    // Interactive UI required (for Authorization Code flow)
    // Redirect to interactive login
}
```

## Certificate Issues
```csharp
catch (MsalClientException ex) when (ex.ErrorCode.Contains("certificate"))
{
    // Certificate not found, invalid, or expired
    logger.LogError("Certificate issue: Check certificate path and validity");
}
```

## Logging Best Practices

Implement `IIdentityLogger` interface for proper logging:

```csharp
using Microsoft.IdentityModel.Abstractions;

class MyIdentityLogger : IIdentityLogger
{
    public EventLogLevel MinLogLevel { get; }

    public MyIdentityLogger()
    {
        // Retrieve log level from environment variable (recommended)
        var msalEnvLogLevel = Environment.GetEnvironmentVariable("MSAL_LOG_LEVEL");
        
        if (Enum.TryParse(msalEnvLogLevel, out EventLogLevel msalLogLevel))
        {
            MinLogLevel = msalLogLevel;
        }
        else
        {
            // Default to Informational (recommended for production)
            MinLogLevel = EventLogLevel.Informational;
        }
    }

    public bool IsEnabled(EventLogLevel eventLogLevel)
    {
        return eventLogLevel <= MinLogLevel;
    }

    public void Log(LogEntry entry)
    {
        // Log to your destination (console, file, etc.)
        Console.WriteLine($"[{entry.EventLogLevel}] {entry.Message}");
    }
}

// Use in your application
MyIdentityLogger logger = new MyIdentityLogger();
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(cert)
    .WithLogging(logger, enablePiiLogging: false)  // Set to true only if needed
    .Build();
```

## Logging Levels

- **LogAlways**: Base level - important health metrics
- **Critical**: Unrecoverable crashes
- **Error**: Debugging and problem identification
- **Warning**: Diagnostics without necessarily an error (recommended minimum for production)
- **Informational**: Informational events
- **Verbose**: Full library behavior details (only for temporary debugging)
