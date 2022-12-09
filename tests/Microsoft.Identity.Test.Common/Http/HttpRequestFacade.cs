// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Microsoft.Identity.Test.Common.Http
{
    public class HttpRequestFacade
    {
        public string Uri { get; set; }

        public HttpMethod Method { get; set; }
        public string ContentForPost { get; set; }
        public List<KeyValuePair<string, string>> Headers { get; set; }

        public HttpRequestMessage ToHttpRequestMessage()
        {
            var httpRequestMessage = new HttpRequestMessage(Method, Uri);
            foreach (var header in Headers)
            {
                httpRequestMessage.Headers.Add(header.Key, header.Value);
            }

            if (Method == HttpMethod.Post)
            {
                httpRequestMessage.Content = new StringContent(ContentForPost);
            }

            return httpRequestMessage;
        }

        public FormUrlEncodedContent ToFormUrlEncodedContent()
        {
            var values = new Dictionary<string, string>
            {
                {"Uri", Uri},
                {"Method", Method.ToString()},
                {"ContentForPost", ContentForPost},
                {"Headers", string.Join(";", Headers.Select(h => $"{h.Key}={h.Value}"))}
            };

            var content = new FormUrlEncodedContent(values);
            return content;
        }

        public static HttpRequestFacade FromHttpRequestMessage(HttpRequestMessage requestMessage)
        {
            var httpRequestFacade = new HttpRequestFacade
            {
                Uri = requestMessage.RequestUri.AbsoluteUri,
                Method = requestMessage.Method,
                Headers = requestMessage.Headers.SelectMany(h => h.Value, (h, v) => new KeyValuePair<string, string>(h.Key, v)).ToList()
            };

            if (requestMessage.Content != null)
            {
                httpRequestFacade.ContentForPost = requestMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            return httpRequestFacade;
        }
    }
}
