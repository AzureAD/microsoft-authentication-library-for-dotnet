// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal.Credential;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.Credential
{
    internal sealed class CredentialIssuerClient
    {
        private static readonly Uri s_endpoint =
            new("http://169.254.169.254/metadata/identity/issuecredential?api-version=2025-04-01-preview");

        private readonly IHttpManager _http;
        private readonly ILoggerAdapter _log;
        private readonly IRetryPolicy _retry;

        public CredentialIssuerClient(IHttpManager http, ILoggerAdapter log, IRetryPolicyFactory f)
        {
            _http = http;
            _log = log;
            _retry = f.GetRetryPolicy(RequestType.Imds);
        }

        internal async Task<ManagedIdentityCredentialResponse> IssueAsync(
            string csr, 
            string attestationEndpoint, 
            RequestContext rc, 
            CancellationToken ct)
        {
            var client = new OAuth2Client(_log, _http, null);
            client.AddHeader("Metadata", "true");
            client.AddBodyParameter("csr", csr);

            if (!string.IsNullOrEmpty(attestationEndpoint))
            {
                string maaToken = await AttestationClient.GetTokenAsync(attestationEndpoint, null, ct)
                                                         .ConfigureAwait(false);
                client.AddBodyParameter("attestation_token", maaToken);
            }

            return await client.GetCredentialResponseAsync(
                        s_endpoint,
                        rc,                 // ← use the passed-in context
                        ct).ConfigureAwait(false);
        }

    }
}
