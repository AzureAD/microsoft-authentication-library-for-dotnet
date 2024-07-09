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
        private readonly Uri _baseEndpoint;

        public HttpMethod Method { get; }

        public IDictionary<string, string> Headers { get; }

        public IDictionary<string, string> BodyParameters { get; }

        public IDictionary<string, string> QueryParameters { get; }

        public ManagedIdentityRequest(HttpMethod method, Uri endpoint)
        {
            Method = method;
            _baseEndpoint = endpoint;
            Headers = new Dictionary<string, string>();
            BodyParameters = new Dictionary<string, string>();
            QueryParameters = new Dictionary<string, string>();
        }

        public Uri ComputeUri()
        {
            UriBuilder uriBuilder = new(_baseEndpoint);
            uriBuilder.AppendQueryParameters(QueryParameters);

            return uriBuilder.Uri;
        }
    }
}
