// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
 * CANONICAL CREDENTIAL RESOLUTION MATRIX
 * ======================================
 * This matrix defines all supported combinations of credential inputs and authentication modes.
 * Each credential implementation enforces its supported modes; unsupported combinations throw MsalClientException.
 * 
 * | Input         | Mode      | Output            | Implementation                        |
 * |---------------|-----------|-------------------|---------------------------------------|
 * | X509Cert      | Regular   | JWT               | CertificateAndClaimsClientCredential  |
 * | X509Cert      | MtlsMode  | certificate       | CertificateAndClaimsClientCredential  |
 * | callback X509 | Regular   | JWT               | CertificateAndClaimsClientCredential  |
 * | callback X509 | MtlsMode  | certificate       | CertificateAndClaimsClientCredential  |
 * | secret        | Regular   | client_secret     | SecretStringClientCredential          |
 * | secret        | MtlsMode  | NOT SUPPORTED     | SecretStringClientCredential (throws) |
 * | jwt           | Regular   | client_assertion  | SignedAssertionClientCredential       |
 * | jwt           | MtlsMode  | NOT SUPPORTED     | SignedAssertionClientCredential (throws) |
 * | jwt + cert    | Regular   | NOT SUPPORTED     | ClientAssertionDelegateCredential (throws) |
 * | jwt + cert    | MtlsMode  | jwt-pop + cert    | ClientAssertionDelegateCredential     |
 * 
 * OUTPUT DETAILS:
 * - "JWT" → TokenRequestParameters contains client_assertion_type=jwt-bearer + client_assertion
 * - "certificate" → TokenRequestParameters is EMPTY (not null), ResolvedCertificate is set
 * - "client_secret" → TokenRequestParameters contains client_secret
 * - "jwt-pop + cert" → TokenRequestParameters contains client_assertion_type=jwt-pop + client_assertion, ResolvedCertificate is set
 * 
 * GUARANTEES:
 * - TokenRequestParameters is NEVER null (may be empty for certificate-only auth)
 * - ResolvedCertificate is set whenever a certificate was obtained (regardless of mode)
 * - Unsupported combinations throw MsalClientException during resolution
 * 
 * TESTS:
 * - See CredentialMatrixTests.cs for comprehensive test coverage of all matrix rows
 */

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    // This file serves as documentation for the credential resolution matrix.
    // Actual implementation logic is in individual credential classes.
}
