// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ManagedIdentityRequest
    {
        public Uri Endpoint { get; private set; }

        private Uri BaseEndpoint;

        public HttpMethod Method { get; }

        public IDictionary<string, string> Headers { get; }

        public IDictionary<string, string> BodyParameters { get; }

        public IDictionary<string, string> QueryParameters { get; }

        public ManagedIdentityRequest(HttpMethod method, Uri endpoint)
        {
            Method = method;
            BaseEndpoint = endpoint;
            Headers = new Dictionary<string, string>();
            BodyParameters = new Dictionary<string, string>();
            QueryParameters = new Dictionary<string, string>();
        }

        public void ComputeUri()
        {
            UriBuilder uriBuilder = new UriBuilder(BaseEndpoint);
            uriBuilder.AppendQueryParameters(QueryParameters);
            Endpoint= uriBuilder.Uri;
        }
    }
}
