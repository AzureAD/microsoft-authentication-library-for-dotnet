// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ManagedIdentityRequest
    {
        private readonly Uri _baseEndpoint;

        public HttpMethod Method { get; }

        public Dictionary<string, string> Headers { get; }

        public Dictionary<string, string> BodyParameters { get; }

        public Dictionary<string, string> QueryParameters { get; }

        public StringContent Content { get; set; }

        public X509Certificate2 BindingCertificate { get; internal set; }

        public ManagedIdentityRequest(
            HttpMethod method, 
            Uri endpoint, 
            StringContent content = null, 
            X509Certificate2 bindingCertificate = null)
        {
            Method = method;
            _baseEndpoint = endpoint;
            Headers = new Dictionary<string, string>();
            BodyParameters = new Dictionary<string, string>();
            QueryParameters = new Dictionary<string, string>();
            Content = content;
            BindingCertificate = bindingCertificate;
        }

        public Uri ComputeUri()
        {
            UriBuilder uriBuilder = new(_baseEndpoint);
            uriBuilder.AppendQueryParameters(QueryParameters);

            return uriBuilder.Uri;
        }
    }
}
