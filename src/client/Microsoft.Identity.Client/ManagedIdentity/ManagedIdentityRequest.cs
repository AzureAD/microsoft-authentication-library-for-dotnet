// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ManagedIdentityRequest(
        HttpMethod method,
        Uri endpoint,
        RequestType requestType = RequestType.ManagedIdentityDefault,
        X509Certificate2 mtlsCertificate = null)
    {
        private readonly Uri _baseEndpoint = endpoint;

        public HttpMethod Method { get; } = method;

        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        public IDictionary<string, string> BodyParameters { get; } = new Dictionary<string, string>();

        public IDictionary<string, string> QueryParameters { get; } = new Dictionary<string, string>();

        public RequestType RequestType { get; set; } = requestType;

        public X509Certificate2 MtlsCertificate { get; set; } = mtlsCertificate;

        public Uri ComputeUri()
        {
            UriBuilder uriBuilder = new(_baseEndpoint);
            uriBuilder.AppendQueryParameters(QueryParameters);

            return uriBuilder.Uri;
        }

        internal void AddClaimsAndCapabilities(
            IEnumerable<string> clientCapabilities,
            AcquireTokenForManagedIdentityParameters parameters,
            ILoggerAdapter logger)
        {
            // xms_cc  – client capabilities
            if (clientCapabilities != null && clientCapabilities.Any())
            {
                QueryParameters["xms_cc"] = string.Join(",", clientCapabilities);
                logger.Info("[Managed Identity] Adding client capabilities (xms_cc) to Managed Identity request.");
            }

            // token_sha256_to_refresh – only when both claims and hash are present
            if (!string.IsNullOrEmpty(parameters.Claims) &&
                !string.IsNullOrEmpty(parameters.RevokedTokenHash))
            {
                QueryParameters["token_sha256_to_refresh"] = parameters.RevokedTokenHash;
                logger.Info("[Managed Identity] Passing SHA-256 of the 'revoked' token to Managed Identity endpoint.");
            }
        }

        /// <summary>
        /// Adds extra query parameters to the Managed Identity request.
        /// </summary>
        /// <param name="extraQueryParameters">Dictionary containing additional query parameters to append to the request.
        /// The parameter can be null.</param>
        /// <param name="logger">Logger instance for recording the operation.</param>
        internal void AddExtraQueryParams(IDictionary<string, string> extraQueryParameters, ILoggerAdapter logger)
        {
            if (extraQueryParameters != null)
            {
                foreach (KeyValuePair<string, string> kvp in extraQueryParameters)
                {
                    QueryParameters[kvp.Key] = kvp.Value;
                }

                logger.Info($"[Managed Identity] Adding {extraQueryParameters.Count} extra query parameters to Managed Identity request.");
            }
        }
    }
}
