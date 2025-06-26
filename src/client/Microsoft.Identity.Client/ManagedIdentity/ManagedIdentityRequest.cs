﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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
        public string Content { get; set; }

        public RequestType RequestType { get; set; }

        public ManagedIdentityRequest(HttpMethod method, Uri endpoint, RequestType requestType = RequestType.ManagedIdentityDefault)
        {
            Method = method;
            _baseEndpoint = endpoint;
            Headers = new Dictionary<string, string>();
            BodyParameters = new Dictionary<string, string>();
            QueryParameters = new Dictionary<string, string>();
            RequestType = requestType;
        }

        public Uri ComputeUri()
        {
            UriBuilder uriBuilder = new(_baseEndpoint);
            uriBuilder.AppendQueryParameters(QueryParameters);

            return uriBuilder.Uri;
        }

        public HttpContent CreateHttpContent()
        {
            if (!string.IsNullOrEmpty(Content))
            {
                return new StringContent(Content, Encoding.UTF8, "application/json");
            }

            if (BodyParameters.Count > 0)
            {
                var formData = new FormUrlEncodedContent(BodyParameters);
                return formData;
            }

            return null; // No body content
        }
    }
}
