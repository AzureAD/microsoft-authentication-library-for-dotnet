// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Http
{
    /// <summary>
    /// Factory to return the instance of HttpManager based on retry configuration and type of MSAL application.
    /// </summary>
    internal sealed class HttpManagerFactory
    {
        public static IHttpManager GetHttpManager(
            IMsalHttpClientFactory httpClientFactory,
            bool withRetry,
            bool isManagedIdentity)
        {
            if (!withRetry)
            {
                return new HttpManager(httpClientFactory, new NoRetryPolicy());
            }

            return isManagedIdentity ?
                new HttpManager(httpClientFactory, new LinearRetryPolicy(1000, 3, HttpRetryConditions.ManagedIdentity)) :
                new HttpManager(httpClientFactory, new LinearRetryPolicy(1000, 1, HttpRetryConditions.Sts));
        }
    }
}
