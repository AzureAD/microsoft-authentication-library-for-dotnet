// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Factory responsible for creating HttpClient. 
    /// See https://learn.microsoft.com/dotnet/api/system.net.http.httpclient?view=net-7.0#instancing for more details.
    /// </summary>
    /// <remarks>
    /// Implementations must be thread safe. 
    /// Do not create a new HttpClient for each call to <see cref="GetHttpClient"/> - this leads to socket exhaustion.
    /// If your app uses Integrated Windows Authentication, ensure <see cref="HttpClientHandler.UseDefaultCredentials"/> is set to true.
    /// For federation metadata (MEX) and WS-Trust requests, implement <see cref="IMsalWsTrustHttpClientFactory"/>
    /// as well. Otherwise, MSAL uses cached platform-default clients for these requests, without the supplied
    /// factory's custom proxy, certificates, handlers, or timeout settings. Other requests are unaffected.
    /// </remarks>
    public interface IMsalHttpClientFactory
    {
        /// <summary>
        /// Method returning an HTTP client that will be used to
        /// communicate with Azure AD. This enables advanced scenarios.
        /// See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <returns>An HTTP client.</returns>
        HttpClient GetHttpClient();
    }
}
