// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Http
{
    /// <summary>
    /// Factory to return the instance of HttpManager based on type of MSAL application.
    /// </summary>
    internal sealed class HttpManagerFactory
    {
        private const int DEFAULT_MANAGED_IDENTITY_RETRY_DELAY_MS = 1000;
        private const int DEFAULT_MANAGED_IDENTITY_MAX_RETRIES = 3;
        private const int DEFAULT_ESTS_RETRY_DELAY_MS = 1000;
        private const int DEFAULT_ESTS_MAX_RETRIES = 1;

        public static IHttpManager GetHttpManager(
            IMsalHttpClientFactory httpClientFactory,
            bool disableInternalRetries = false)
        {
            return new HttpManager(httpClientFactory, disableInternalRetries);
        }
    }
}
