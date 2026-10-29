// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Creates HTTP clients for federation metadata (MEX) and WS-Trust requests.
    /// </summary>
    /// <remarks>
    /// Implement this interface on the factory supplied to MSAL to customize MEX and WS-Trust transport.
    /// If the supplied factory does not implement this interface, MSAL uses cached platform-default clients
    /// for these requests instead. Other requests continue to use the supplied factory.
    /// Implementations must be thread-safe and reuse clients, keeping different credential configurations
    /// isolated. Configure handlers before their first request; do not modify shared clients or handlers
    /// when this method is called. MSAL does not dispose the returned clients.
    /// The client's <see cref="HttpClient.Timeout"/> applies to each HTTP attempt, and MSAL's retry
    /// budget restarts for each redirect hop. There is no additional timeout for the entire redirect chain.
    /// </remarks>
    public interface IMsalWsTrustHttpClientFactory : IMsalHttpClientFactory
    {
        /// <summary>
        /// Returns a client with automatic redirects disabled and the requested server credential policy.
        /// MSAL validates and follows redirects itself.
        /// </summary>
        /// <param name="useDefaultCredentials">
        /// Whether to use the current user's default credentials for server authentication on platforms that
        /// support Integrated Windows Authentication. When false, the client must not supply server
        /// authentication credentials, including credentials configured by the application.
        /// This does not disable authentication to a configured proxy.
        /// </param>
        /// <returns>
        /// A reusable HTTP client whose handler has <see cref="HttpClientHandler.AllowAutoRedirect"/> set
        /// to false (or the platform equivalent), and <see cref="HttpClientHandler.UseDefaultCredentials"/>
        /// configured according to <paramref name="useDefaultCredentials"/> where supported.
        /// All delegating handlers must also leave redirects to MSAL and respect the credential policy.
        /// </returns>
        HttpClient GetHttpClient(bool useDefaultCredentials);
    }
}
