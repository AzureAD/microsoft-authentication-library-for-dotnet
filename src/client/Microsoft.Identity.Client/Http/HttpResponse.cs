// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Linq;

namespace Microsoft.Identity.Client.Http
{
    internal class HttpResponse
    {
        public HttpResponseHeaders Headers { get; set; }

        public IDictionary<string, string> HeadersAsDictionary
        {
            get
            {
                var headers = new Dictionary<string, string>();

                if (Headers != null)
                {
                    foreach (var kvp in Headers)
                    {
                        headers[kvp.Key] = kvp.Value.First();
                    }
                }
                return headers;
            }
        }

        public HttpStatusCode StatusCode { get; set; }

        public string UserAgent { get; set; }

        public string Body { get; set; }
    }
}
