// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal static class ImdsComputeMetadataManager
    {
        private static readonly Lazy<IHttpManager> s_httpManager = new Lazy<IHttpManager>(
            () => HttpManagerFactory.GetHttpManager(
                PlatformProxyFactory.CreatePlatformProxy(null).CreateDefaultHttpClientFactory()));

        internal static async Task<ComputeMetadataResponse> GetComputeMetadataAsync(
            CancellationToken cancellationToken)
        {
            return await GetComputeMetadataAsync(s_httpManager.Value, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task<ComputeMetadataResponse> GetComputeMetadataAsync(
            IHttpManager httpManager,
            CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" }
            };

            string queryParams = $"{ImdsManagedIdentitySource.ApiVersionQueryParam}={ImdsManagedIdentitySource.ImdsComputeApiVersion}";
            Uri endpoint = ImdsManagedIdentitySource.GetValidatedEndpoint(
                new NullLogger(),
                ImdsManagedIdentitySource.ImdsComputePath,
                queryParams);

            HttpResponse response;
            try
            {
                response = await httpManager.SendRequestAsync(
                    endpoint,
                    headers,
                    body: null,
                    method: HttpMethod.Get,
                    logger: new NullLogger(),
                    doNotThrow: true,
                    mtlsCertificate: null,
                    validateServerCertificate: null,
                    cancellationToken: cancellationToken,
                    retryPolicy: new ImdsRetryPolicy())
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            return JsonHelper.TryToDeserializeFromJson<ComputeMetadataResponse>(response.Body);
        }
    }
}
