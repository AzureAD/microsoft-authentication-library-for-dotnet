# Client-Assertion API

## Goal  
Expose **one forward-compatible API** that lets a confidential client provide  

1. A signed client-assertion (JWT).  
2. *(Optional)* the X-509 certificate to which that JWT is bound (for mTLS PoP).

---

## Public API

```csharp
public class AssertionResponse
{
    /// <summary>Base-64 JWT that MSAL sends as <c>client_assertion</c>.</summary>
    public string Assertion { get; init; } 
    
    /// <summary>
    /// Certificate for mutual-TLS PoP.  
    /// Leave <c>null</c> for a bearer assertion.
    /// </summary>
    public X509Certificate2 TokenBindingCertificate { get; init; }

    // Future properties (AssertionType, FmiPath, ExtraClaims, …) can be added
}

public ConfidentialClientApplicationBuilder WithClientAssertion(
    Func<AssertionRequestOptions,
         CancellationToken,
         Task<AssertionResponse>> assertionDelegateAsync);
```

## Behavior

If Certificate is supplied

- MSAL pins the certificate to the mutual‑TLS channel.
- MSAL sends the JWT with client_assertion_type = jwt-pop.

If Certificate is null, 
- MSAL sends the JWT as a bearer assertion (jwt-bearer).

## Future Enhancements & mTLS Support

- AssertionResponse is an extensible container; new assertion flavours (e.g., FMI‑bound tokens, custom claim sets) can surface via additional properties—no new builder overloads required.
- When a certificate is present, MSAL automatically performs the mutual‑TLS handshake and forwards the JWT as a PoP (jwt-pop) client‑assertion.
