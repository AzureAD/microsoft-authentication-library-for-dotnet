# Certificate with SNI (Subject Name Identifier) Setup

## Overview
SNI adds an additional layer of security by including the certificate chain (x5c claim) in the client assertion sent to Azure AD using the `sendX5C: true` parameter.

## When to Use SNI
- **High-security scenarios** - Services with strict security requirements
- **Multi-tenant applications** - Enhanced isolation between tenants
- **SaaS platforms** - When serving many customers
- **Zero-trust model** - Enhanced proof of possession

## How to Use with sendX5C=true
```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(cert, sendX5C: true)  // Enables x5c=true (sends certificate chain in assertion)
    .Build();
```

## What sendX5C Does
- Includes the full certificate chain (x5c claim) in the JWT assertion
- Provides stronger binding between certificate and application
- Enables Azure AD to validate certificate chain integrity
- Prevents unauthorized certificate reuse

## Benefits
- Stronger proof of possession
- Certificate chain validation at token endpoint
- Defense against certificate misuse if leaked
- Works across all flows (Auth Code, OBO, Client Credentials)

## Configuration Requirements
- Valid certificate with private key
- Certificate registered in Azure AD/Entra ID
- Azure AD must be configured to validate x5c

## Comparison: Standard Certificate vs SNI
| Aspect | Standard | SNI (sendX5C: true) |
|--------|----------|---------|
| Security | Good | Enhanced |
| Setup | Simple | Simple |
| Performance | Minimal overhead | Minimal overhead |
| Proof of Possession | Basic | Strong |
| Recommended for | Most scenarios | High-security systems |
