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
    /// Custom factories own their HTTP transport behavior. For WS-Trust scenarios, they must enforce equivalent
    /// redirect and credential-target policies that prevent credentials from being sent to insecure or untrusted
    /// redirect targets.
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
