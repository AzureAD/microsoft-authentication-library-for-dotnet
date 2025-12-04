// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ImdsManagedIdentitySource : AbstractManagedIdentity
    {
        // IMDS constants. Docs for IMDS are available here https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/how-to-use-vm-token#get-a-token-using-http
        // used in unit tests as well
        public const string ApiVersionQueryParam = "api-version";
        public const string DefaultImdsBaseEndpoint= "http://169.254.169.254";
        public const string ImdsApiVersion = "2018-02-01";
        public const string ImdsTokenPath = "/metadata/identity/oauth2/token";

        private const string DefaultMessage = "[Managed Identity] Service request failed.";

        internal const string IdentityUnavailableError = "[Managed Identity] Authentication unavailable. " +
            "Either the requested identity has not been assigned to this resource, or other errors could " +
            "be present. Ensure the identity is correctly assigned and check the inner exception for more " +
            "details. For more information, visit https://aka.ms/msal-managed-identity.";

        internal const string GatewayError = "[Managed Identity] Authentication unavailable. The request failed due to a gateway error.";

        private readonly Uri _imdsEndpoint;

        private static string s_cachedBaseEndpoint = null;

        public static AbstractManagedIdentity Create(RequestContext requestContext)
        {
            return new ImdsManagedIdentitySource(requestContext);
        }

        internal ImdsManagedIdentitySource(RequestContext requestContext) : 
            base(requestContext, ManagedIdentitySource.Imds)
        {
            requestContext.Logger.Info(() => "[Managed Identity] Defaulting to IMDS endpoint for managed identity.");

            _imdsEndpoint = GetValidatedEndpoint(requestContext.Logger, ImdsTokenPath);

            requestContext.Logger.Verbose(() => "[Managed Identity] Creating IMDS managed identity source. Endpoint URI: " + _imdsEndpoint);
        }

        protected override Task<ManagedIdentityRequest> CreateRequestAsync(string resource)
        {
            ManagedIdentityRequest request = new(HttpMethod.Get, _imdsEndpoint);

            request.Headers.Add("Metadata", "true");
            request.QueryParameters[ApiVersionQueryParam] = ImdsApiVersion;
            request.QueryParameters["resource"] = resource;

            switch (_requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                case AppConfig.ManagedIdentityIdType.ClientId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned client id to the request.");
                    request.QueryParameters[Constants.ManagedIdentityClientId] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;

                case AppConfig.ManagedIdentityIdType.ResourceId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned resource id to the request.");
                    request.QueryParameters[Constants.ManagedIdentityResourceIdImds] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;

                case AppConfig.ManagedIdentityIdType.ObjectId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned object id to the request.");
                    request.QueryParameters[Constants.ManagedIdentityObjectId] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;
            }

            var userAssignedIdQueryParam = GetUserAssignedIdQueryParam(
                _requestContext.ServiceBundle.Config.ManagedIdentityId.IdType,
                _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId,
                _requestContext.Logger,
                imdsV1: true);
            if (userAssignedIdQueryParam != null)
            {
                request.QueryParameters[userAssignedIdQueryParam.Value.Key] = userAssignedIdQueryParam.Value.Value;
            }

            request.RequestType = RequestType.Imds;

            return Task.FromResult(request);
        }

        public static KeyValuePair<string, string>? GetUserAssignedIdQueryParam(
            AppConfig.ManagedIdentityIdType idType,
            string userAssignedId,
            ILoggerAdapter logger,
            bool imdsV1 = false)
        {
            switch (idType)
            {
                case AppConfig.ManagedIdentityIdType.ClientId:
                    logger?.Info("[Managed Identity] Adding user assigned client id to the request.");
                    return new KeyValuePair<string, string>(Constants.ManagedIdentityClientId, userAssignedId);

                case AppConfig.ManagedIdentityIdType.ResourceId:
                    logger?.Info("[Managed Identity] Adding user assigned resource id to the request.");
                    return new KeyValuePair<string, string>(imdsV1 ? Constants.ManagedIdentityResourceIdImds : Constants.ManagedIdentityResourceId, userAssignedId);

                case AppConfig.ManagedIdentityIdType.ObjectId:
                    logger?.Info("[Managed Identity] Adding user assigned object id to the request.");
                    return new KeyValuePair<string, string>(Constants.ManagedIdentityObjectId, userAssignedId);

                default:
                    return null;
            }
        }

        protected override async Task<ManagedIdentityResponse> HandleResponseAsync(
            AcquireTokenForManagedIdentityParameters parameters,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            // handle error status codes indicating managed identity is not available
            var baseMessage = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => IdentityUnavailableError,
                HttpStatusCode.BadGateway => GatewayError,
                HttpStatusCode.GatewayTimeout => GatewayError,
                _ => default
            };

            if (baseMessage != null)
            {
                string message = CreateRequestFailedMessage(response, baseMessage);

                var errorContentMessage = GetMessageFromErrorResponse(response);

                message = message + Environment.NewLine + errorContentMessage;

                _requestContext.Logger.Error($"Error message: {message} Http status code: {response.StatusCode}");

                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    message,
                    null,
                    ManagedIdentitySource.Imds,
                    null);

                throw exception;
            }

            // Default behavior to handle successful scenario and general errors.
            return await base.HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);
        }

        internal static string CreateRequestFailedMessage(HttpResponse response, string message)
        {
            StringBuilder messageBuilder = new StringBuilder();

            messageBuilder
                .AppendLine(message ?? DefaultMessage)
                .Append("Status: ")
                .Append(response.StatusCode.ToString());

            if (response.Body != null)
            {
                messageBuilder
                    .AppendLine()
                    .AppendLine("Content:")
                    .AppendLine(response.Body);
            }

            messageBuilder
                .AppendLine()
                .AppendLine("Headers:");

            foreach (var header in response.HeadersAsDictionary)
            {
                messageBuilder.AppendLine($"{header.Key}: {header.Value}");
            }

            return messageBuilder.ToString();
        }

        public static Uri GetValidatedEndpoint(
            ILoggerAdapter logger,
            string subPath,
            string queryParams = null
            )
        {
            if (s_cachedBaseEndpoint == null)
            {
                if (!string.IsNullOrEmpty(EnvironmentVariables.PodIdentityEndpoint))
                {
                    logger.Verbose(() => "[Managed Identity] Environment variable AZURE_POD_IDENTITY_AUTHORITY_HOST for IMDS returned endpoint: " + EnvironmentVariables.PodIdentityEndpoint);
                    s_cachedBaseEndpoint = EnvironmentVariables.PodIdentityEndpoint;
                }
                else
                {
                    logger.Verbose(() => "[Managed Identity] Unable to find AZURE_POD_IDENTITY_AUTHORITY_HOST environment variable for IMDS, using the default endpoint.");
                    s_cachedBaseEndpoint = DefaultImdsBaseEndpoint;
                }
            }
            
            UriBuilder builder = new UriBuilder(s_cachedBaseEndpoint)
            {
                Path = subPath
            };
            
            if (!string.IsNullOrEmpty(queryParams))
            {
                builder.Query = queryParams;
            }

            return builder.Uri;
        }

        public static string ImdsQueryParamsHelper(
            RequestContext requestContext,
            string apiVersionQueryParam,
            string imdsApiVersion)
        {
            var queryParams = $"{apiVersionQueryParam}={imdsApiVersion}";

            var userAssignedIdQueryParam = GetUserAssignedIdQueryParam(
                requestContext.ServiceBundle.Config.ManagedIdentityId.IdType,
                requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId,
                requestContext.Logger);

            if (userAssignedIdQueryParam != null)
            {
                queryParams += $"&{userAssignedIdQueryParam.Value.Key}={userAssignedIdQueryParam.Value.Value}";
            }

            return queryParams;
        }

        public static async Task<bool> ProbeImdsEndpointAsync(
            RequestContext requestContext,
            ImdsVersion imdsVersion)
        {
            string apiVersionQueryParam;
            string imdsApiVersion;
            string imdsEndpoint;
            string imdsStringHelper;

            switch (imdsVersion)
            {
                case ImdsVersion.V2:
#if NET462
                requestContext.Logger.Info("[Managed Identity] IMDSv2 flow is not supported on .NET Framework 4.6.2. Cryptographic operations required for managed identity authentication are unavailable on this platform. Skipping IMDSv2 probe.");
                return false;
#else
                    apiVersionQueryParam = ImdsV2ManagedIdentitySource.ApiVersionQueryParam;
                    imdsApiVersion = ImdsV2ManagedIdentitySource.ImdsV2ApiVersion;
                    imdsEndpoint = ImdsV2ManagedIdentitySource.CsrMetadataPath;
                    imdsStringHelper = "IMDSv2";
                    break;
#endif
                case ImdsVersion.V1:
                    apiVersionQueryParam = ApiVersionQueryParam;
                    imdsApiVersion = ImdsApiVersion;
                    imdsEndpoint = ImdsTokenPath;
                    imdsStringHelper = "IMDSv1";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(imdsVersion), imdsVersion, null);
            }

            var queryParams = ImdsQueryParamsHelper(requestContext, apiVersionQueryParam, imdsApiVersion);

            var headers = new Dictionary<string, string>
            {
                { OAuth2Header.XMsCorrelationId, requestContext.CorrelationId.ToString() }
            };

            IRetryPolicyFactory retryPolicyFactory = requestContext.ServiceBundle.Config.RetryPolicyFactory;
            IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.ImdsProbe);

            HttpResponse response = null;

            try
            {
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                {
                    response = await requestContext.ServiceBundle.HttpManager.SendRequestAsync(
                        GetValidatedEndpoint(requestContext.Logger, imdsEndpoint, queryParams),
                        headers,
                        body: null,
                        method: HttpMethod.Get,
                        logger: requestContext.Logger,
                        doNotThrow: false,
                        mtlsCertificate: null,
                        validateServerCertificate: null,
                        cancellationToken: timeoutCts.Token,
                        retryPolicy: retryPolicy)
                    .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                requestContext.Logger.Info($"[Managed Identity] {imdsStringHelper} probe endpoint failure. Exception occurred while sending request to probe endpoint: {ex}");
                return false;
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                requestContext.Logger.Info(() => $"[Managed Identity] {imdsStringHelper} managed identity is available.");
                return true;
            }
            else
            {
                requestContext.Logger.Info(() => $"[Managed Identity] {imdsStringHelper} managed identity is not available. Status code: {response.StatusCode}, Body: {response.Body}");
                return false;
            }
        }
    }
}
