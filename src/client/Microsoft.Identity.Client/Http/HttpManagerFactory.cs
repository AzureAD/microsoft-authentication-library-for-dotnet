﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Http
{
    /// <summary>
    /// Factory to return the instance of HttpManager based on retry configuration and type of MSAL application.
    /// </summary>
    internal sealed class HttpManagerFactory
    {
        public static IHttpManager GetHttpManager(
            IMsalHttpClientFactory httpClientFactory,
            bool isManagedIdentity = false,
            bool withRetry = true)
        {
            return new HttpManager(httpClientFactory, isManagedIdentity, withRetry);
        }
    }
}
