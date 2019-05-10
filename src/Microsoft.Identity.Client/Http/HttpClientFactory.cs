// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Net.Http.Headers;
#if iOS
using Foundation;
using UIKit;
#endif

namespace Microsoft.Identity.Client.Http
{
    internal class HttpClientFactory : IMsalHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        // The HttpClient is a singleton per ClientApplication so that we don't have a process wide singleton.
        public const long MaxResponseContentBufferSizeInBytes = 1024 * 1024;

        public HttpClientFactory()
        {
#if iOS
            // See https://aka.ms/msal-net-httpclient for details
            if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
            {
                _httpClient = new HttpClient(new NSUrlSessionHandler())
                {
                    MaxResponseContentBufferSize = MaxResponseContentBufferSizeInBytes
                };
            }
#elif ANDROID
            // See https://aka.ms/msal-net-httpclient for details
            _httpClient = new HttpClient(new Xamarin.Android.Net.AndroidClientHandler());
#else
            _httpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true })
            {
                MaxResponseContentBufferSize = MaxResponseContentBufferSizeInBytes
            };
#endif
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public HttpClient GetHttpClient()
        {
            return _httpClient;
        }
    }
}
