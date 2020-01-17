// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Factory responsible for creating HttpClient
    /// .Net recommends to use a single instance of HttpClient
    /// </summary>
    /// <remarks>
    /// Implementations must be thread safe. Consider creating and configuring an HttpClient in the constructor
    /// of the factory, and returning the same object in <see cref="GetHttpClient"/>
    /// </remarks>
    public interface IMsalHttpClientFactory
    {
        /// <summary>
        /// Method returning an Http client that will be used to
        /// communicate with Azure AD. This enables advanced scenarios.
        /// See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <returns>An Http client</returns>
        HttpClient GetHttpClient();
    }
}
