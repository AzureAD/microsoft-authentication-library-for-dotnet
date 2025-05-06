// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Http
{
    /// <summary>
    /// Factory to return the instance of HttpManager based on type of MSAL application.
    /// </summary>
    internal sealed class HttpManagerFactory
    {
        public static IHttpManager GetHttpManager(
            IMsalHttpClientFactory httpClientFactory,
            bool disableInternalRetries = false)
        {
            return new HttpManager(httpClientFactory, disableInternalRetries);
        }
    }
}
