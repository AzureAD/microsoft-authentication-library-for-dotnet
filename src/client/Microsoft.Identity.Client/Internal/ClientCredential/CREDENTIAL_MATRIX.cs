// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
 * CANONICAL CREDENTIAL RESOLUTION MATRIX
 * 
 * This file documents all supported combinations of credential inputs and authentication modes
 * with their expected outputs. This serves as the single source of truth for credential behavior.
 * 
 * LEGEND:
 * -------
 * Input Types:
 *   - Secret: Client secret string
 *   - Cert: X509Certificate2 (static)
 *   - CertCallback: Func<AssertionRequestOptions, Task<X509Certificate2>>
 *   - AssertionString: Static signed JWT string
 *   - AssertionCallback: Func<AssertionRequestOptions, Task<string>>
 *   - AssertionCallbackWithCert: Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>>
 * 
 * Modes:
 *   - Regular: Standard OAuth2 authentication
 *   - MtlsMode: Mutual TLS authentication with certificate binding
 * 
 * Output Fields:
 *   - client_secret: OAuth2 client secret parameter
 *   - client_assertion: JWT assertion string
 *   - client_assertion_type: "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" or "urn:ietf:params:oauth:client-assertion-type:jwt-pop"
 *   - ResolvedCertificate: X509Certificate2 for mTLS handshake
 *   - CredentialSource: Static or Callback
 * 
 * MATRIX:
 * -------
 * 
 * ROW 1: Client Secret + Regular Mode
 *   Input: Secret
 *   Mode: Regular
 *   Output:
 *     - client_secret: <secret>
 *     - CredentialSource: Static
 * 
 * ROW 2: Client Secret + MtlsMode
 *   Input: Secret
 *   Mode: MtlsMode
 *   Output: ERROR - Client secret cannot be used with mTLS
 * 
 * ROW 3: Static Certificate + Regular Mode
 *   Input: Cert
 *   Mode: Regular
 *   Output:
 *     - client_assertion: <signed JWT>
 *     - client_assertion_type: jwt-bearer
 *     - ResolvedCertificate: <cert>
 *     - CredentialSource: Static
 * 
 * ROW 4: Static Certificate + MtlsMode
 *   Input: Cert
 *   Mode: MtlsMode
 *   Output:
 *     - ResolvedCertificate: <cert> (for mTLS handshake only, no assertion)
 *     - CredentialSource: Static
 * 
 * ROW 5: Certificate Callback + Regular Mode
 *   Input: CertCallback
 *   Mode: Regular
 *   Output:
 *     - client_assertion: <signed JWT from resolved cert>
 *     - client_assertion_type: jwt-bearer
 *     - ResolvedCertificate: <cert from callback>
 *     - CredentialSource: Callback
 * 
 * ROW 6: Certificate Callback + MtlsMode
 *   Input: CertCallback
 *   Mode: MtlsMode
 *   Output:
 *     - ResolvedCertificate: <cert from callback> (for mTLS handshake only, no assertion)
 *     - CredentialSource: Callback
 * 
 * ROW 7: Static Assertion String + Regular Mode
 *   Input: AssertionString
 *   Mode: Regular
 *   Output:
 *     - client_assertion: <assertion>
 *     - client_assertion_type: jwt-bearer
 *     - CredentialSource: Static
 * 
 * ROW 8: Static Assertion String + MtlsMode
 *   Input: AssertionString
 *   Mode: MtlsMode
 *   Output: ERROR - Static assertions cannot provide certificate for mTLS
 * 
 * ROW 9: Assertion Callback (string) + Regular Mode
 *   Input: AssertionCallback
 *   Mode: Regular
 *   Output:
 *     - client_assertion: <assertion from callback>
 *     - client_assertion_type: jwt-bearer
 *     - CredentialSource: Callback
 * 
 * ROW 10: Assertion Callback (string) + MtlsMode
 *   Input: AssertionCallback
 *   Mode: MtlsMode
 *   Output: ERROR - String assertion callback cannot provide certificate for mTLS
 * 
 * ROW 11: Assertion Callback (ClientSignedAssertion, no cert) + Regular Mode
 *   Input: AssertionCallbackWithCert returning ClientSignedAssertion(assertion, null)
 *   Mode: Regular
 *   Output:
 *     - client_assertion: <assertion>
 *     - client_assertion_type: jwt-bearer
 *     - CredentialSource: Callback
 * 
 * ROW 12: Assertion Callback (ClientSignedAssertion, no cert) + MtlsMode
 *   Input: AssertionCallbackWithCert returning ClientSignedAssertion(assertion, null)
 *   Mode: MtlsMode
 *   Output: ERROR - mTLS mode requires certificate but callback returned null
 * 
 * ROW 13: Assertion Callback (ClientSignedAssertion, with cert) + Regular Mode
 *   Input: AssertionCallbackWithCert returning ClientSignedAssertion(assertion, cert)
 *   Mode: Regular
 *   Output:
 *     - client_assertion: <assertion>
 *     - client_assertion_type: jwt-pop (implicit mTLS due to cert presence)
 *     - ResolvedCertificate: <cert>
 *     - CredentialSource: Callback
 * 
 * ROW 14: Assertion Callback (ClientSignedAssertion, with cert) + MtlsMode
 *   Input: AssertionCallbackWithCert returning ClientSignedAssertion(assertion, cert)
 *   Mode: MtlsMode
 *   Output:
 *     - client_assertion: <assertion>
 *     - client_assertion_type: jwt-pop
 *     - ResolvedCertificate: <cert>
 *     - CredentialSource: Callback
 * 
 * ROW 15: Request-Level Assertion Override + Regular Mode
 *   Input: App-level credential + Request-level assertion callback override
 *   Mode: Regular
 *   Output: Request-level credential takes precedence; behavior follows rows 9-14 based on request-level type
 * 
 * ROW 16: Request-Level Assertion Override + MtlsMode
 *   Input: App-level credential + Request-level assertion callback override
 *   Mode: MtlsMode
 *   Output: Request-level credential takes precedence; behavior follows rows 9-14 based on request-level type
 * 
 * VALIDATION RULES:
 * -----------------
 * 1. Exactly one of client_secret OR client_assertion must be present (never both, never neither)
 * 2. If client_assertion is present, client_assertion_type must also be present
 * 3. client_assertion_type must be either "jwt-bearer" or "jwt-pop"
 * 4. ResolvedCertificate is optional and used for mTLS handshake
 * 5. In MtlsMode with certificate credentials, no client_assertion is generated (cert-only auth)
 * 6. Request-level credentials always override app-level credentials
 * 7. String assertion callbacks (Func<AssertionRequestOptions, Task<string>>) cannot return certificates
 * 8. ClientSignedAssertion callbacks can optionally return a certificate for mTLS binding
 * 
 * DOUBLE-INVOKE PREVENTION:
 * -------------------------
 * The CredentialMaterialResolver ensures that each credential is invoked exactly once per token request.
 * All credential callbacks (CertCallback, AssertionCallback, AssertionCallbackWithCert) are invoked
 * via GetCredentialMaterialAsync() which is called only once, and the result is cached for that request.
 * 
 * BACKWARD COMPATIBILITY:
 * -----------------------
 * The legacy AddConfidentialClientParametersAsync() method remains on IClientCredential for backward
 * compatibility but is being replaced by GetCredentialMaterialAsync(). Both methods coexist during
 * the transition period.
 */

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// This class exists solely for documentation purposes and is not instantiated.
    /// See the comments above for the complete credential resolution matrix.
    /// </summary>
    internal static class CREDENTIAL_MATRIX
    {
        // This class intentionally left empty - documentation is in comments above
    }
}
