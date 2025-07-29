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

        /// <summary>
        /// Calls the IMDSv2 metadata endpoint. In discovery mode, it returns null if the endpoint is not available (i.e. not running on IMDSv2).
        /// In non-discovery mode, it throws exceptions.
        /// </summary>
        public static async Task<CsrMetadata> GetCsrMetadataAsync(RequestContext requestContext, bool probeMode)
        {
            string uami = null;
            switch (requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                case AppConfig.ManagedIdentityIdType.ClientId:
                    uami = requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    requestContext.Logger.Info("[Managed Identity] ImdsV2 supports user-assigned client id.");
                    break;

                // TODO bogdan: fix this as per spec
                //case AppConfig.ManagedIdentityIdType.ResourceId:
                //    requestContext.Logger.Info("[Managed Identity] ImdsV2 doesn't support user-assigned resource id. Either provide a client id, or use a system-assigned managed identity.");
                //    return false;

                //case AppConfig.ManagedIdentityIdType.ObjectId:
                //    requestContext.Logger.Info("[Managed Identity] ImdsV2 doesn't support user-assigned object id. Either provide a client id, or use a system-assigned managed identity.");
                //    return false;

                default:
                    requestContext.Logger.Info("[Managed Identity] ImdsV2 supports system-assigned managed identity.");
                    break;
            }

            string queryParams = $"api-version={ImdsManagedIdentitySource.ImdsApiVersion}";
            if (!string.IsNullOrEmpty(uami))
            {
                queryParams += $"&uaid={uami}"; // TODO bogdan: this is not per spec
            }

            Uri csrMetadataEndpoint = ImdsManagedIdentitySource.GetValidatedEndpoint(requestContext.Logger, CsrMetadataPath, queryParams);

            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" },
                { "x-ms-client-request-id", requestContext.CorrelationId.ToString() }
            };

            IRetryPolicyFactory retryPolicyFactory = requestContext.ServiceBundle.Config.RetryPolicyFactory;
            IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.ManagedIdentityDefault);

            // CSR metadata GET request
            HttpResponse response;

            // TODO: Remove try/catch once we have a mock for this request, and create a helper method for the SendRequestAsync
            try
            {
                response = await requestContext.ServiceBundle.HttpManager.SendRequestAsync(
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
            }
            catch (Exception ex)
            {
                // try/catch is for testing purposes, to avoid adding a mock for this request
                if (probeMode)
                {
                    requestContext.Logger.Info(() => "[Managed Identity] IMSv2 CSR endpoint failure. Exception occurred while sending request to CSR metadata endpoint: " + ex);
                    return null; 
                }
                else
                {
                    throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                        MsalError.ManagedIdentityRequestFailed,
                        "[Imdsv2] ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed.",
                        ex,
                        ManagedIdentitySource.ImdsV2,
                        null);
                }                
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (probeMode)
                {
                    requestContext.Logger.Info(() => $"[Managed Identity] IMDSV2 managed identity is not available. Status code: {response.StatusCode}, Body: {response.Body}");
                    return null; 
                }
                else
                {
                    throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                        MsalError.ManagedIdentityRequestFailed,
                        $"[Imdsv2] ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed due to HTTP error. Status code: {response.StatusCode} Body: {response.Body}",
                        null,
                        ManagedIdentitySource.ImdsV2,
                        (int)response.StatusCode);
                }
            }

            ValidateCsrMetadataResponse(response, requestContext.Logger);
            

            var csrMetadata = TryCreateCsrMetadata(response.Body, requestContext.Logger);
            return csrMetadata;
        }

        private static void ValidateCsrMetadataResponse( // TODO bogdan: should this have a probe mode?
            HttpResponse response,
            ILoggerAdapter logger)
        {
            /*
             * Match "IMDS/" at start of "server" header string (`^IMDS\/`)
             * Match the first three numbers with dots (`\d+.\d+.\d+.`)
             * Capture the last number in a group (`(\d+)`)
             * Ensure end of string (`$`)
             *
             * Example:
             * [
             * "IMDS/150.870.65.1556",  // index 0: full match
             * "1556"                   // index 1: captured group (\d+)
             * ]
             */
            string serverHeader = response.HeadersAsDictionary.TryGetValue("server", out var value) ? value : null;
            if (serverHeader == null)
            {
                logger.Info(() => "[Managed Identity] IMDSV2 managed identity is not available. 'server' header is missing from the CSR metadata response.");
                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                       MsalError.ManagedIdentityRequestFailed,
                       $"[Imdsv2] ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed because response doesn't have server header.  Status code: {response.StatusCode} Body: {response.Body}",
                       null,
                       ManagedIdentitySource.ImdsV2,
                       (int)response.StatusCode);
            }

            var match = System.Text.RegularExpressions.Regex.Match(
                serverHeader,
                @"^IMDS/\d+\.\d+\.\d+\.(\d+)$"
            );
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out int version) || version <= 1324)
            {
                logger.Info(() => $"[Managed Identity] IMDSV2 managed identity is not available. 'server' header format/version invalid. Extracted version: {match.Groups[1].Value}");
                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                       MsalError.ManagedIdentityRequestFailed,
                       $"[Imdsv2] ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed because response doesn't have server header.  Status code: {response.StatusCode} Body: {response.Body}",
                       null,
                       ManagedIdentitySource.ImdsV2,
                       (int)response.StatusCode);
            }
        }

        private static CsrMetadata TryCreateCsrMetadata(
            String responseBody,
            ILoggerAdapter logger)
        {
            CsrMetadataResponse csrMetadataResponse = JsonHelper.DeserializeFromJson<CsrMetadataResponse>(responseBody);
            CsrMetadata csrMetadata = CsrMetadata.TryCreate(csrMetadataResponse, logger);
            if (csrMetadata == null)
            {
                logger.Info(() => "[Managed Identity] IMDSV2 managed identity is not available. Invalid CsrMetadata response.");
                return null;
            }

            logger.Info(() => "[Managed Identity] IMDSV2 managed identity is available.");
            return csrMetadata;
        }

        public static AbstractManagedIdentity Create(RequestContext requestContext)
        {
            return new ImdsV2ManagedIdentitySource(requestContext);
        }

        internal ImdsV2ManagedIdentitySource(RequestContext requestContext) :
            base(requestContext, ManagedIdentitySource.ImdsV2)
        {
            
        }

        // TODO: Implement CreateRequest
        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            throw new NotImplementedException(); // TODO: should we have a basic request or reuse IMDSv1 for testing?
        }
    }
}
