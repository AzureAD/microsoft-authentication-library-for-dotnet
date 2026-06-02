// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Fetches compute metadata from the Azure Instance Metadata Service (IMDS)
    /// to determine VM characteristics such as OS type and security profile.
    /// </summary>
    internal static class ImdsComputeMetadataManager
    {
        internal const string ImdsComputePath = "/metadata/instance/compute";
        internal const string ImdsComputeApiVersion = "2021-02-01";

        internal static async Task<ComputeMetadataResponse> GetComputeMetadataAsync(
            IHttpManager httpManager,
            ILoggerAdapter logger,
            CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" }
            };

            try
            {
                string queryParams =
                    $"{ImdsManagedIdentitySource.ApiVersionQueryParam}={ImdsComputeApiVersion}";

                Uri endpoint = ImdsManagedIdentitySource.GetValidatedEndpoint(
                    logger,
                    ImdsComputePath,
                    queryParams);

                HttpResponse response = await httpManager.SendRequestAsync(
                    endpoint,
                    headers,
                    body: null,
                    method: HttpMethod.Get,
                    logger: logger,
                    doNotThrow: true,
                    mtlsCertificate: null,
                    validateServerCertificate: null,
                    cancellationToken: cancellationToken,
                    retryPolicy: new ImdsRetryPolicy())
                    .ConfigureAwait(false);

                if (response is null || response.StatusCode != HttpStatusCode.OK)
                {
                    logger.Info($"[Managed Identity] IMDS compute metadata request failed. " +
                        $"StatusCode: {response?.StatusCode}");
                    return null;
                }

                return JsonHelper.TryToDeserializeFromJson<ComputeMetadataResponse>(response.Body);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Info($"[Managed Identity] IMDS compute metadata request failed with exception: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Determines whether the host VM supports mTLS PoP based on compute metadata.
        /// mTLS PoP is supported when the VM runs Windows and is a TVM (TrustedLaunch) or CVM (ConfidentialVM).
        /// </summary>
        internal static bool IsMtlsPopSupported(ComputeMetadataResponse metadata)
        {
            if (metadata is null)
            {
                return false;
            }

            bool isWindows = string.Equals(metadata.OsType, "Windows", StringComparison.OrdinalIgnoreCase);

            string securityType = metadata.SecurityProfile?.SecurityType;
            bool isTvmOrCvm = string.Equals(securityType, "TrustedLaunch", StringComparison.OrdinalIgnoreCase)
                || string.Equals(securityType, "ConfidentialVM", StringComparison.OrdinalIgnoreCase);

            return isWindows && isTvmOrCvm;
        }
    }
}
