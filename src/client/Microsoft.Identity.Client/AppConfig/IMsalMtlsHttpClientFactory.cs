// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Factory responsible for creating HttpClient with a cert handler for MTLS. 
    /// See https://learn.microsoft.com/dotnet/api/system.net.http.httpclient?view=net-7.0#instancing for more details.
    /// </summary>
    /// <remarks>
    /// Implementations must be thread safe. 
    /// Do not create a new HttpClient for each call to <see cref="GetHttpClient(X509Certificate2)"/> - this leads to socket exhaustion.
    /// If your app uses Integrated Windows Authentication, ensure <see cref="HttpClientHandler.UseDefaultCredentials"/> is set to true.
    /// </remarks>
    public interface IMsalMtlsHttpClientFactory : IMsalHttpClientFactory
    {
        /// <summary>
        /// Method returning an HTTP client that will be used to
        /// communicate with Azure AD over MTLS. This enables advanced scenarios.
        /// See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <returns>An HTTP client with an certificate collection on the handler.</returns>
        HttpClient GetHttpClient(X509Certificate2 x509Certificate2);

    }

}
