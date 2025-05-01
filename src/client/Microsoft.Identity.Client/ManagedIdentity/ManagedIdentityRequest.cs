// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ManagedIdentityRequest
    {
        // referenced in unit tests, cannot be private
        public const int DefaultManagedIdentityMaxRetries = 3;
        // this will be overridden in the unit tests so that they run faster
        public static int DefaultManagedIdentityRetryDelayMs { get; set; } = 1000;

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

            IRetryPolicy defaultRetryPolicy = new LinearRetryPolicy(
                DefaultManagedIdentityRetryDelayMs,
                DefaultManagedIdentityMaxRetries,
                HttpRetryConditions.ManagedIdentity);
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
