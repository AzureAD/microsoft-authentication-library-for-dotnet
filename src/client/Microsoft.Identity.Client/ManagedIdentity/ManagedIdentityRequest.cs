// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ManagedIdentityRequest
    {
        public UriBuilder UriBuilder { get; }

        public HttpMethod Method { get; }

        public IDictionary<string, string> Headers { get; }

        public IDictionary<string, string> BodyParameters { get; } = new Dictionary<string, string>();

        public ManagedIdentityRequest(HttpMethod method, Uri endpoint)
        {
            Method = method;
            UriBuilder = new UriBuilder(endpoint);
            Headers = new Dictionary<string, string>();
        }
        
    }
}
