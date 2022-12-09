// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Linq;

namespace Microsoft.Identity.Test.Common.Http
{
    public class HttpResponseFacade
    {
        public List<KeyValuePair<string, string>> Headers { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public string Content { get; set; }

        public HttpResponseMessage ToHttpResponseMessage()
        {
            var response = new HttpResponseMessage(HttpStatusCode);
            foreach (var header in Headers)
            {
                response.Headers.Add(header.Key, header.Value);
            }

            response.Content = new StringContent(Content);
            return response;
        }

        public static HttpResponseFacade FromHttpResponseMessage(HttpResponseMessage response)
        {
            var facade = new HttpResponseFacade
            {
                HttpStatusCode = response.StatusCode,
                Headers = response.Headers.SelectMany(h => h.Value, (h, v) => new KeyValuePair<string, string>(h.Key, v)).ToList(),
                Content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            };

            return facade;
        }
    }
}
