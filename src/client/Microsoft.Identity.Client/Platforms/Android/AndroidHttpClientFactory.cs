// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.Platforms.Android
{
    class AndroidHttpClientFactory : IMsalHttpClientFactory
    {
        public HttpClient GetHttpClient()
        {
            // Continue to create HttpClient for each PublicClientApplication
            // as static instance seems to have problems 
            // https://forums.xamarin.com/discussion/144802/do-you-use-singleton-httpclient-or-dispose-create-new-instance-every-time

            var httpClient = new HttpClient(
                new Xamarin.Android.Net.AndroidMessageHandler());
            HttpClientConfig.ConfigureRequestHeadersAndSize(httpClient);
            return httpClient;
        }
    }
}
