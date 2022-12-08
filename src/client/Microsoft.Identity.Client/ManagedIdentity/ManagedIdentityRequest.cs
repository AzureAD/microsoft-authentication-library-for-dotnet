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
        public Lazy<Uri> Endpoint => new Lazy<Uri>(() => ComputeUri(this)) ;

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

        public static Uri ComputeUri(ManagedIdentityRequest managedIdentityRequest)
        {
            UriBuilder uriBuilder = new(managedIdentityRequest._baseEndpoint);
            uriBuilder.AppendQueryParameters(managedIdentityRequest.QueryParameters);

            return uriBuilder.Uri;
        }
    }
}
