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
    /// 
    /// </summary>
    internal sealed class HttpManagerFactory
    {
        public static IHttpManager GetHttpManager(IMsalHttpClientFactory httpClientFactory, bool retryConfig, bool isManagedIdentity)
        {
            if (!retryConfig)
            {
                return new HttpManager(httpClientFactory);
            }

            return isManagedIdentity ?
                new HttpManagerManagedIdentity(httpClientFactory) :
                new HttpManagerWithRetry(httpClientFactory);
        }
    }
}
