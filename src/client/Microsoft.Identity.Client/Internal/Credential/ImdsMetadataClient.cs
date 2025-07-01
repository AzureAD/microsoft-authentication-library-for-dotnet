// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Credential
{
    /// <summary>GET /metadata/identity/getPlatformMetadata …</summary>
    internal interface IMdsMetadataClient
    {
        Task<PlatformMetadataResponse> GetAsync(CancellationToken ct);
    }

    internal sealed class ImdsMetadataClient : IMdsMetadataClient
    {
        private static readonly Uri s_endpoint =
            new("http://169.254.169.254/metadata/identity/getPlatformMetadata?api-version=2025-04-01-preview");

        private readonly IHttpManager _http;
        private readonly ILoggerAdapter _log;
        private readonly IRetryPolicy _retry;

        public ImdsMetadataClient(IHttpManager http, ILoggerAdapter log, IRetryPolicyFactory f)
        {
            _http = http;
            _log = log;
            _retry = f.GetRetryPolicy(RequestType.Imds);
        }

        public async Task<PlatformMetadataResponse> GetAsync(CancellationToken ct)
        {
            HttpResponse resp = await _http.SendRequestAsync(
                                    s_endpoint,
                                    new Dictionary<string, string> { { "Metadata", "true" } },
                                    body: null,
                                    method: HttpMethod.Get,
                                    logger: _log,
                                    doNotThrow: false,
                                    mtlsCertificate: null,
                                    validateServerCertificate: null,
                                    cancellationToken: ct,
                                    retryPolicy: _retry).ConfigureAwait(false);

            return JsonHelper.DeserializeFromJson<PlatformMetadataResponse>(resp.Body);
        }
    }
}
