// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Http
{
    /// <summary>
    /// Factory to return the instance of HttpManager based on type of MSAL application.
    /// </summary>
    internal sealed class HttpManagerFactory
    {
        // referenced in unit tests, cannot be private
        public const int DEFAULT_MANAGED_IDENTITY_MAX_RETRIES = 3;
        public const int DEFAULT_ESTS_MAX_RETRIES = 1;

        // these will be overridden in the unit tests so that they run faster
        public static int DEFAULT_MANAGED_IDENTITY_RETRY_DELAY_MS { get; set; } = 1000;
        public static int DEFAULT_ESTS_RETRY_DELAY_MS { get; set; } = 1000;

        public static IHttpManager GetHttpManager(
            IMsalHttpClientFactory httpClientFactory,
            bool disableInternalRetries = false)
        {
            return new HttpManager(httpClientFactory, disableInternalRetries);
        }
    }
}
