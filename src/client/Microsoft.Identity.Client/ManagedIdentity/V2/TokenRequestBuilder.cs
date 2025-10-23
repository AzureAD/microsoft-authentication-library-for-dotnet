// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.OAuth2.Throttling;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal static class TokenRequestBuilder
    {
        public static ManagedIdentityRequest BuildTokenRequest(
            RequestContext ctx,
            string resource,
            string mtlsEndpoint,
            string tenantId,
            string clientId,
            string tokenType,
            X509Certificate2 mtlsCert)
        {
            var request = new ManagedIdentityRequest(
                HttpMethod.Post,
                new Uri($"{mtlsEndpoint}/{tenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}"));

            Dictionary<string, string> idParams = MsalIdHelper.GetMsalIdParameters(ctx.Logger);

            foreach (KeyValuePair<string, string> p in idParams)
            {
                request.Headers[p.Key] = p.Value;
            }

            request.Headers.Add(OAuth2Header.XMsCorrelationId, ctx.CorrelationId.ToString());
            request.Headers.Add(ThrottleCommon.ThrottleRetryAfterHeaderName, ThrottleCommon.ThrottleRetryAfterHeaderValue);
            request.Headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");

            request.BodyParameters.Add("client_id", clientId);
            request.BodyParameters.Add("grant_type", OAuth2GrantType.ClientCredentials);
            request.BodyParameters.Add("scope", resource.TrimEnd('/') + "/.default");
            request.BodyParameters.Add("token_type", tokenType);

            request.RequestType = RequestType.STS;
            request.MtlsCertificate = mtlsCert;
            return request;
        }
    }
}
