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

        public IDictionary<string, string> Headers { get; }

        public IDictionary<string, string> BodyParameters { get; }

        public IDictionary<string, string> QueryParameters { get; }

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

#if TRA
        public string GetCredentialCacheKey()
        {
            Uri uri = _baseEndpoint;
            string queryString = uri.Query; 

            // Use HttpUtility to parse the query string and get the managed identity id parameters
            string clientId = HttpUtility.ParseQueryString(queryString).Get("client_id");
            string resourceId = HttpUtility.ParseQueryString(queryString).Get("mi_res_id");
            string objectId = HttpUtility.ParseQueryString(queryString).Get("object_id");

            if (!string.IsNullOrEmpty(clientId))
            {
                return clientId;
            }
            else if (!string.IsNullOrEmpty(resourceId))
            {
                return resourceId;
            }
            else if (!string.IsNullOrEmpty(objectId))
            {
                return objectId;
            }
            else
            {
                return Constants.ManagedIdentityDefaultClientId; 
            }
        }
#endif
    }
}
