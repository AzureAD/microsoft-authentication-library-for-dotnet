// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Utils;
using static Microsoft.Identity.Client.Http.Retry.DefaultRetryPolicy;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ManagedIdentityRequest
    {
        private readonly Uri _baseEndpoint;

        public HttpMethod Method { get; }

        public IDictionary<string, string> Headers { get; }

        public IDictionary<string, string> BodyParameters { get; }

        public IDictionary<string, string> QueryParameters { get; }

        public IRetryPolicy RetryPolicy { get; set; }

        public ManagedIdentityRequest(HttpMethod method, Uri endpoint, IRetryPolicy retryPolicy = null)
        {
            Method = method;
            _baseEndpoint = endpoint;
            Headers = new Dictionary<string, string>();
            BodyParameters = new Dictionary<string, string>();
            QueryParameters = new Dictionary<string, string>();

            IRetryPolicy defaultRetryPolicy = new DefaultRetryPolicy(RequestType.ManagedIdentity);
            RetryPolicy = retryPolicy ?? defaultRetryPolicy;
        }

        public Uri ComputeUri()
        {
            UriBuilder uriBuilder = new(_baseEndpoint);
            uriBuilder.AppendQueryParameters(QueryParameters);

            return uriBuilder.Uri;
        }
    }
}
