// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TestOnly
{
    /// <summary>
    /// Constant values used across managed-identity test scenarios.
    /// </summary>
    public static class ManagedIdentityTestConstants
    {
        /// <summary>
        /// Valid DER base-64 test certificate that expires in 2125.
        /// Can be used in mock responses or for certificate-binding tests.
        /// </summary>
        public const string ValidTestCertificate =
            "MIIDATCCAemgAwIBAgIUSfjghyQB4FIS41rWfNcZHTLE/R4wDQYJKoZIhvcNAQELBQAwDzENMAsGA1UEAwwEVGVzdDAgFw0yNTA4MjgyMDIxMDBaGA8yMTI1MDgwNDIwMjEwMFowDzENMAsGA1UEAwwEVGVzdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALlc0S6TdwgQKGRl3Y/9uWNRpWo1WHiZtd1YdgCBt0rjxTqsbQUurU0B9Kdk7QQ9srxmjimxGHaUFypbb39awqIdQQcuQvIUj5+sQh9zzCyR35bGQp8vwbna5GlhAIbzsUi/y5kEGUMbuQN05XfoJSQrU35XZ8duQSDH5h9aDr6kuLcpDHo9/9vZiosPfqGPxZGtVjMvrJdVQGLJF35xD3LlX8xG2iJfVK/xYQVi3MgbRNQaL2lHtZaGAc1CToMUPO60xXrZkQE08hC907YTBcavUVQg4vrOaPpsCs+Fj6EJcasADAJeh1mGBn3kHFPCxBa2MKFraFPp53zOagTvYV0CAwEAAaNTMFEwHQYDVR0OBBYEFA9irQR/O6/V2JVyDEHFOdUDjAsyMB8GA1UdIwQYMBaAFA9irQR/O6/V2JVyDEHFOdUDjAsyMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQELBQADggEBAAOxtgYjtkUDVvWzq/lkjLTdcLjPvmH0hF34A3uvX4zcjmqF845lfvszTuhc1mx5J6YLEzKfr4TrO3D3g2BnDLvhupok0wEmJ9yVwbt1laim7zP09gZqnUqYM9hYKDhwgLZAaG3zGNocxDEAU7jazMGOGF7TweB7LdNuVI6CqgDOBQ8Cy2ObuZvzCI5Y7f+HucXpiJOu1xNa2ZZpMpQycYEvi5TD+CL5CBv2fcKQRn/+u5B3ZXCD2C9jT/RZ7rH46mIG7nC7dS4J2o4JjmlJIUAe2U6tRay5GvEmc/nZK8hd9y4BICzrykp9ENAoy9i+uaE1GGWeNgO+irrcrAcLwto=";

        /// <summary>
        /// Expired DER base-64 test certificate for testing error scenarios.
        /// Created September 8, 2025 with 1-day validity — always expired.
        /// </summary>
        public const string ExpiredTestCertificate =
            "MIIC/zCCAeegAwIBAgIUGSVU23Wc0+QtCbUTjsyPOrc0XpEwDQYJKoZIhvcNAQELBQAwDzENMAsGA1UEAwwEVGVzdDAeFw0yNTA5MDgyMjAxMTdaFw0yNTA5MDkyMjAxMTdaMA8xDTALBgNVBAMMBFRlc3QwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQC5XNEuk3cIEChkZd2P/bljUaVqNVh4mbXdWHYAgbdK48U6rG0FLq1NAfSnZO0EPbK8Zo4psRh2lBcqW29/WsKiHUEHLkLyFI+frEIfc8wskd+WxkKfL8G52uRpYQCG87FIv8uZBBlDG7kDdOV36CUkK1N+V2fHbkEgx+YfWg6+pLi3KQx6Pf/b2YqLD36hj8WRrVYzL6yXVUBiyRd+cQ9y5V/MRtoiX1Sv8WEFYtzIG0TUGi9pR7WWhgHNQk6DFDzutMV62ZEBNPIQvdO2EwXGr1FUIOL6zmj6bArPhY+hCXGrAAwCXodZhgZ95BxTwsQWtjCha2hT6ed8zmoE72FdAgMBAAGjUzBRMB0GA1UdDgQWBBQPYq0Efzuv1diVcgxBxTnVA4wLMjAfBgNVHSMEGDAWgBQPYq0Efzuv1diVcgxBxTnVA4wLMjAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUAA4IBAQCXAD7cjWmmTqP0NX4MqwO0AHtO+KGVtfxF8aI21Ty/nHh2SAODzsemP3NBBvoEvllwtcVyutPqvUiAflMLNbp0ucTu+aWE14s1V9Bnt6++5g7gtXItsNV3F/ymYKsyfhDvJbWCOv5qYeJMQ+jtODHN9qnATODT5voULTwEVSYQXtutwRxR8e70Cvok+F+4I6Ni49DJ8DmcYzvB94uthqpDsygY1vYzpRbB5hpW0/D7kgVVWyWoOWiE1mV7Fry7tUWQw7EqnX89kMLMy4g6UfOv4gtam8RBa9dLyMW1rCHRxOulP47joI10g9JoJ9DssiQTUojJgQXOSBBXdD20H+zl";

        /// <summary>
        /// System-assigned managed identity client ID used in mock IMDS v2 responses.
        /// This is the default <c>clientId</c> embedded in CSR and certificate mock responses.
        /// </summary>
        public const string SystemAssignedManagedIdentityClientId = "d3adb33f-c0de-ed0c-c0de-deadb33fc0d3";

        /// <summary>
        /// Tenant ID used in mock IMDS v2 responses and token endpoints.
        /// </summary>
        public const string TestTenantId = "751a212b-4003-416e-b600-e1f48e40db9f";

        /// <summary>
        /// Mock mTLS authentication endpoint returned by <see cref="MockHelpers.MockCertificateRequestResponse"/>.
        /// This matches the value that MSAL expects to call for Entra token acquisition.
        /// </summary>
        public const string MtlsAuthenticationEndpoint = "http://fake_mtls_authentication_endpoint";

        /// <summary>
        /// IMDS endpoint base URL used in mock handlers.
        /// </summary>
        public const string ImdsEndpoint = "http://169.254.169.254/metadata/identity/";

        /// <summary>
        /// Access token value embedded in successful mock MSI responses.
        /// </summary>
        public const string TestAccessToken = "some-access-token";
    }
}
