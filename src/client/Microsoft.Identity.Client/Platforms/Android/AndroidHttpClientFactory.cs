// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.Platforms.Android
{
    class AndroidHttpClientFactory :
        IMsalWsTrustHttpClientFactory
    {
        private readonly Lazy<HttpClient> _wsTrustHttpClient =
            new Lazy<HttpClient>(() => GetHttpClient(allowAutoRedirect: false));

        public HttpClient GetHttpClient()
        {
            return GetHttpClient(allowAutoRedirect: true);
        }

        HttpClient IMsalWsTrustHttpClientFactory.GetHttpClient(bool useDefaultCredentials)
        {
            // Android does not use Windows default credentials.
            return _wsTrustHttpClient.Value;
        }

        private static HttpClient GetHttpClient(bool allowAutoRedirect)
        {
            // Continue to create HttpClient for each PublicClientApplication
            // as static instance seems to have problems 
            // https://forums.xamarin.com/discussion/144802/do-you-use-singleton-httpclient-or-dispose-create-new-instance-every-time

            var handler = new Xamarin.Android.Net.AndroidMessageHandler
            {
                AllowAutoRedirect = allowAutoRedirect
            };
            var httpClient = new HttpClient(handler);
            HttpClientConfig.ConfigureRequestHeadersAndSize(httpClient);
            return httpClient;
        }
    }
}
