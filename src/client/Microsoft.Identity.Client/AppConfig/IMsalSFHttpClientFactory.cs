// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Factory responsible for creating HttpClient with a custom server certificate validation callback.
    /// This is useful for the Service Fabric scenario where the server certificate validation is required using the server cert.
    /// See https://learn.microsoft.com/dotnet/api/system.net.http.httpclient?view=net-7.0#instancing for more details.
    /// </summary>
    /// <remarks>
    /// Implementations must be thread safe. 
    /// Do not create a new HttpClient for each call to <see cref="GetHttpClient"/> - this leads to socket exhaustion.
    /// If your app uses Integrated Windows Authentication, ensure <see cref="HttpClientHandler.UseDefaultCredentials"/> is set to true.
    /// </remarks>
    public interface IMsalSFHttpClientFactory : IMsalHttpClientFactory
    {

        /// <summary>
        /// Method returning an HTTP client that will be used to validate the server certificate through the provided callback.
        /// This method is useful when custom certificate validation logic is required, 
        /// for the managed identity flow running on a service fabric cluster.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns>An HTTP client configured with the provided server certificate validation callback.</returns>
        HttpClient GetHttpClient(HttpClientHandler handler);
    }
}
