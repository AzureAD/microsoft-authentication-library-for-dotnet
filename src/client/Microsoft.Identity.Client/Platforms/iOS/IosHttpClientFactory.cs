// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Foundation;
using Microsoft.Identity.Client.Http;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class IosHttpClientFactory :
        IMsalWsTrustHttpClientFactory
    {
        private readonly Lazy<HttpClient> _wsTrustHttpClient =
            new Lazy<HttpClient>(() => GetHttpClient(allowAutoRedirect: false));
        private readonly Lazy<HttpClient> _wsTrustHttpClientWithoutCredentials =
            new Lazy<HttpClient>(() => GetHttpClient(allowAutoRedirect: false, useDefaultCredentials: false));

        public HttpClient GetHttpClient()
        {
            return GetHttpClient(allowAutoRedirect: true);
        }

        HttpClient IMsalWsTrustHttpClientFactory.GetHttpClient(bool useDefaultCredentials)
        {
            return useDefaultCredentials
                ? _wsTrustHttpClient.Value
                : _wsTrustHttpClientWithoutCredentials.Value;
        }

        private static HttpClient GetHttpClient(bool allowAutoRedirect, bool useDefaultCredentials = true)
        {
            HttpClient httpClient;
            if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
            {
                NSUrlSessionHandler handler;
                if (useDefaultCredentials)
                {
                    handler = new NSUrlSessionHandler();
                }
                else
                {
                    // Do not let the native session supply credentials from shared storage after a redirect.
                    using var configuration = NSUrlSessionConfiguration.DefaultSessionConfiguration;
                    configuration.URLCredentialStorage = null;
                    configuration.TimeoutIntervalForRequest = 24 * 60 * 60;
                    configuration.TimeoutIntervalForResource = 24 * 60 * 60;
                    handler = new NSUrlSessionHandler(configuration);
                }

                handler.AllowAutoRedirect = allowAutoRedirect;
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
