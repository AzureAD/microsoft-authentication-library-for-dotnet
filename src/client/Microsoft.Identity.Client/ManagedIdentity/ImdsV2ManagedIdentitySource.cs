// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ImdsV2ManagedIdentitySource : AbstractManagedIdentity
    {
        private const string CsrMetadataPath = "/metadata/identity/getPlatformMetadata";

        private CsrMetadata _csrMetadata;

        public static async Task<AbstractManagedIdentity> CreateAsync(RequestContext requestContext)
        {
            string uami = null;
            switch (requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                case AppConfig.ManagedIdentityIdType.ClientId:
                    uami = requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;

                case AppConfig.ManagedIdentityIdType.ResourceId:
                    requestContext.Logger.Info("[Managed Identity] ImdsV2 doesn't support user-assigned resource id. Either provide a client id, or use a system assigned managed identity.");
                    return null;

                case AppConfig.ManagedIdentityIdType.ObjectId:
                    requestContext.Logger.Info("[Managed Identity] ImdsV2 doesn't support user-assigned object id. Either provide a client id, or use a system assigned managed identity.");
                    return null;
            }

            string queryParams = $"api-version={ImdsManagedIdentitySource.ImdsApiVersion}";
            if (!string.IsNullOrEmpty(uami))
            {
                queryParams += $"&uaid={uami}";
            }

            Uri csrMetadataEndpoint = ImdsManagedIdentitySource.GetValidatedEndpoint(requestContext.Logger, CsrMetadataPath, queryParams);

            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" },
                { "x-ms-client-request-id", Guid.NewGuid().ToString() }
            };

            IRetryPolicyFactory retryPolicyFactory = requestContext.ServiceBundle.Config.RetryPolicyFactory;
            IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.Imds);

            // CSR metadata GET request
            HttpResponse response = await requestContext.ServiceBundle.HttpManager.SendRequestAsync(
                csrMetadataEndpoint,
                headers,
                body: null,
                method: System.Net.Http.HttpMethod.Get,
                logger: requestContext.Logger,
                doNotThrow: false,
                mtlsCertificate: null,
                validateServerCertificate: null,
                cancellationToken: requestContext.UserCancellationToken,
                retryPolicy: retryPolicy)
            .ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }
            CsrMetadataResponse csrMetadataResponse = JsonHelper.DeserializeFromJson<CsrMetadataResponse>(response.Body);
            CsrMetadata csrMetadata = CsrMetadata.CreateOrNull(csrMetadataResponse, requestContext.Logger);
            if (csrMetadata == null)
            {
                return null;
            }
            
            requestContext.Logger.Info(() => "[Managed Identity] IMDSV2 managed identity is available.");
            return new ImdsV2ManagedIdentitySource(requestContext, csrMetadata);
        }

        internal ImdsV2ManagedIdentitySource(RequestContext requestContext, CsrMetadata csrMetadata) :
            base(requestContext, ManagedIdentitySource.Imds)
        {
            _csrMetadata = csrMetadata;
            requestContext.Logger.Verbose(() => "[Managed Identity] IMDSV2 managed identity is available.");
        }

        // TODO: Implement CreateRequest
        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                "",
                "ImdsV2ManagedIdentitySource.CreateRequest is not implemented yet.",
                null,
                ManagedIdentitySource.Imds,
                null);
        }
    }
}
