// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Identity.Client.Http;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class IosHttpClientFactory :
        IMsalHttpClientFactory,
        IHttpClientFactoryWithRedirectControl
    {
        public HttpClient GetHttpClient()
        {
            return GetHttpClient(allowAutoRedirect: true);
        }

        HttpClient IHttpClientFactoryWithRedirectControl.GetHttpClient(
            bool allowAutoRedirect,
            bool useDefaultCredentials)
        {
            return GetHttpClient(allowAutoRedirect);
        }

        private static HttpClient GetHttpClient(bool allowAutoRedirect)
        {
            HttpClient httpClient;
            if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
            {
                var handler = new NSUrlSessionHandler
                {
                    AllowAutoRedirect = allowAutoRedirect
                };
                httpClient = new HttpClient(handler);
               
            }
            else
            {
                httpClient = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = allowAutoRedirect
                });
            }

            HttpClientConfig.ConfigureRequestHeadersAndSize(httpClient);
            return httpClient;
        }
    }
}
