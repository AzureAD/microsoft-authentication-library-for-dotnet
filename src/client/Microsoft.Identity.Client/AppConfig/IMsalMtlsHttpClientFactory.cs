// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Internal factory responsible for creating HttpClient instances configured for mutual TLS (MTLS).
    /// This factory is specifically intended for use within the MSAL library for secure communication with Azure AD using MTLS.
    /// For more details on HttpClient instancing, see https://learn.microsoft.com/dotnet/api/system.net.http.httpclient?view=net-7.0#instancing.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface must be thread-safe.
    /// It is important to reuse HttpClient instances to avoid socket exhaustion.
    /// Do not create a new HttpClient for each call to <see cref="GetHttpClient(X509Certificate2)"/>.
    /// If your application requires Integrated Windows Authentication, set <see cref="HttpClientHandler.UseDefaultCredentials"/> to true.
    /// This interface is intended for internal use by MSAL only and is designed to support MTLS scenarios.
    /// </remarks>
    internal interface IMsalMtlsHttpClientFactory : IMsalHttpClientFactory
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
