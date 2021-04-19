// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Identity.Client.Http;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class IosHttpClientFactory : IMsalHttpClientFactory
    {
        public HttpClient GetHttpClient()
        {
            HttpClient httpClient;
            if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
            {
                var handler = new NSUrlSessionHandler();
                httpClient = new HttpClient(handler);
               
            }
            else
            {
                httpClient = new HttpClient();
            }

            HttpClientConfig.ConfigureRequestHeadersAndSize(httpClient);
            return httpClient;
        }
    }
}

