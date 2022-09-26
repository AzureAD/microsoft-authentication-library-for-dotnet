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
            // For Mono, continue to create HttpClient for each PublicClientApplication
            // as static instance seems to have problems 
            // https://forums.xamarin.com/discussion/144802/do-you-use-singleton-httpclient-or-dispose-create-new-instance-every-time

            var httpClient = new HttpClient(
            // As per Xamarin guidance https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/http-stack?tabs=windows
#if NET6_0
                new Xamarin.Android.Net.AndroidMessageHandler());
#else
                new Xamarin.Android.Net.AndroidClientHandler());
#endif
            HttpClientConfig.ConfigureRequestHeadersAndSize(httpClient);
            return httpClient;
        }
    }
}
