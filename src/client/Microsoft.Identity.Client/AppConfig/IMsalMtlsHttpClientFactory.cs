// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// A factory responsible for creating HttpClient instances configured for mutual TLS (mTLS).
    /// This factory is intended for use to secure communication with Azure AD using mTLS.
    /// For more details on HttpClient instancing, see https://learn.microsoft.com/dotnet/api/system.net.http.httpclient?view=net-7.0#instancing.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface must be thread-safe.
    /// It is important to reuse HttpClient instances to avoid socket exhaustion.
    /// Do not create a new HttpClient for each call to <see cref="GetHttpClient(X509Certificate2)"/>.
    /// If your application requires Integrated Windows Authentication, set <see cref="HttpClientHandler.UseDefaultCredentials"/> to true.
    /// This interface is designed to support mTLS scenarios.
    /// </remarks>
    public interface IMsalMtlsHttpClientFactory : IMsalHttpClientFactory
    {
        /// <summary>
        /// Returns an HttpClient configured with a certificate for mutual TLS authentication.
        /// This method enables advanced MTLS scenarios within Azure AD communications in MSAL.
        /// </summary>
        /// <param name="x509Certificate2">The certificate to be used for MTLS authentication.</param>
        /// <returns>An HttpClient instance configured with the specified certificate.</returns>
        HttpClient GetHttpClient(X509Certificate2 x509Certificate2);
    }
}
