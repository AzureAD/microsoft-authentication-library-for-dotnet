// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class AzureArcManagedIdentitySource : AbstractManagedIdentity
    {
        private const string ArcApiVersion = "2019-11-01";
        private const string AzureArc = "Azure Arc";

        private readonly Uri _endpoint;

        public static AbstractManagedIdentity Create(RequestContext requestContext)
        {
            string identityEndpoint;
                
            if (EnvironmentVariables.IdentityEndpoint == null)
            {
                identityEndpoint = "http://127.0.0.1:40342/metadata/identity/oauth2/token";
                requestContext.Logger.Info(() => "[Managed Identity] Azure Arc was detected through file based detection but the environment variables were not found. Defaulting to known azure arc endpoint.");
            } else
            {
                identityEndpoint = EnvironmentVariables.IdentityEndpoint;
            }

            requestContext.Logger.Info(() => "[Managed Identity] Azure Arc managed identity is available.");

            if (!Uri.TryCreate(identityEndpoint, UriKind.Absolute, out Uri endpointUri))
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    MsalErrorMessage.ManagedIdentityEndpointInvalidUriError,
                    "IDENTITY_ENDPOINT", identityEndpoint, AzureArc);

                // Use the factory to create and throw the exception
                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.InvalidManagedIdentityEndpoint,
                    errorMessage,
                    null, 
                    ManagedIdentitySource.AzureArc,
                    null); 

                throw exception;
            }

            requestContext.Logger.Verbose(()=>"[Managed Identity] Creating Azure Arc managed identity. Endpoint URI: " + endpointUri);
            return new AzureArcManagedIdentitySource(endpointUri, requestContext);
        }

        private AzureArcManagedIdentitySource(Uri endpoint, RequestContext requestContext) : 
            base(requestContext, ManagedIdentitySource.AzureArc)
        {
            _endpoint = endpoint;
        }

        protected override Task<ManagedIdentityRequest> CreateRequestAsync(string resource)
        {
            ManagedIdentityRequest request = new ManagedIdentityRequest(System.Net.Http.HttpMethod.Get, _endpoint);

            request.Headers.Add("Metadata", "true");
            request.QueryParameters["api-version"] = ArcApiVersion;
            request.QueryParameters["resource"] = resource;

            switch (_requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                case AppConfig.ManagedIdentityIdType.ClientId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned client id to the request.");
                    request.QueryParameters[Constants.ManagedIdentityClientId] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;

                case AppConfig.ManagedIdentityIdType.ResourceId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned resource id to the request.");
                    // Azure Arc honors the IMDS "msi_res_id" spelling for the resource-id selector; the
                    // "mi_res_id" spelling is silently ignored and returns the system-assigned identity.
                    request.QueryParameters[Constants.ManagedIdentityResourceIdImds] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;

                case AppConfig.ManagedIdentityIdType.ObjectId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned object id to the request.");
                    request.QueryParameters[Constants.ManagedIdentityObjectId] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;
            }

            return Task.FromResult(request);
        }

        protected override async Task<ManagedIdentityResponse> HandleResponseAsync(
            AcquireTokenForManagedIdentityParameters parameters,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            _requestContext.Logger.Verbose(() => $"[Managed Identity] Response received. Status code: {response.StatusCode}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (!response.HeadersAsDictionary.TryGetValue("WWW-Authenticate", out string challenge))
                {
                    _requestContext.Logger.Error("[Managed Identity] WWW-Authenticate header is expected but not found.");

                    var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                        MsalError.ManagedIdentityRequestFailed,
                        MsalErrorMessage.ManagedIdentityNoChallengeError,
                        null,
                        ManagedIdentitySource.AzureArc,
                        null);

                    throw exception;
                }

                var splitChallenge = challenge.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                ValidateSplitChallenge(splitChallenge);

                var authHeaderValue = "Basic " + File.ReadAllText(splitChallenge[1]);

                ManagedIdentityRequest request = await CreateRequestAsync(parameters.Resource).ConfigureAwait(false);

                _requestContext.Logger.Verbose(() => "[Managed Identity] Adding authorization header to the request.");
                request.Headers.Add("Authorization", authHeaderValue);

                IRetryPolicyFactory retryPolicyFactory = _requestContext.ServiceBundle.Config.RetryPolicyFactory;
                IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.ManagedIdentityDefault);

                response = await _requestContext.ServiceBundle.HttpManager.SendRequestAsync(
                         request.ComputeUri(),
                         request.Headers,
                         body: null,
                         method: System.Net.Http.HttpMethod.Get,
                         logger: _requestContext.Logger,
                         doNotThrow: true,
                         mtlsCertificate: null,
                         validateServerCertificate: null,
                         cancellationToken: cancellationToken,
                         retryPolicy: retryPolicy)
                    .ConfigureAwait(false);
            }

            ManagedIdentityResponse managedIdentityResponse =
                await base.HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);

            VerifyUserAssignedIdentityWasHonored(managedIdentityResponse);

            return managedIdentityResponse;
        }

        // Azure Arc user-assigned managed identity is a preview capability. A legacy Arc agent
        // ignores the client_id / object_id / msi_res_id selector and silently returns the machine's
        // system-assigned identity. An agent that honors the request echoes the selected identity
        // back in the token response. When the caller asked for a user-assigned identity but the
        // response does not confirm it, fail closed so the caller never receives a token for a
        // different identity than the one requested.
        private void VerifyUserAssignedIdentityWasHonored(ManagedIdentityResponse managedIdentityResponse)
        {
            var managedIdentityId = _requestContext.ServiceBundle.Config.ManagedIdentityId;

            if (!managedIdentityId.IsUserAssigned)
            {
                return;
            }

            string echoedIdentity;
            switch (managedIdentityId.IdType)
            {
                case AppConfig.ManagedIdentityIdType.ClientId:
                    echoedIdentity = managedIdentityResponse.ClientId;
                    break;
                case AppConfig.ManagedIdentityIdType.ResourceId:
                    echoedIdentity = managedIdentityResponse.ResourceId;
                    break;
                case AppConfig.ManagedIdentityIdType.ObjectId:
                    echoedIdentity = managedIdentityResponse.ObjectId;
                    break;
                default:
                    echoedIdentity = null;
                    break;
            }

            // Compare identifiers case-insensitively: client_id / object_id are GUIDs, and an ARM
            // resource id (msi_res_id) can legitimately differ in segment casing.
            if (string.IsNullOrEmpty(echoedIdentity) ||
                !string.Equals(echoedIdentity, managedIdentityId.UserAssignedId, StringComparison.OrdinalIgnoreCase))
            {
                _requestContext.Logger.Error(
                    "[Managed Identity] Azure Arc did not confirm the requested user-assigned identity in the token response. " +
                    "The agent likely does not support user-assigned managed identity and returned the system-assigned identity.");

                throw CreateManagedIdentityException(
                    MsalError.UserAssignedManagedIdentityNotSupported,
                    string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityUserAssignedNotSupported, AzureArc));
            }
        }

        private void ValidateSplitChallenge(string[] splitChallenge)
        {
            if (splitChallenge.Length != 2)
            {
                throw CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    MsalErrorMessage.ManagedIdentityInvalidChallenge);
            }

            _requestContext.Logger.Verbose(() => $"[Managed Identity] Challenge is valid. FilePath: {splitChallenge[1]}");
            
            if (!IsValidPath(splitChallenge[1]))
            {
                throw CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    MsalErrorMessage.ManagedIdentityInvalidFile);
            }

            _requestContext.Logger.Verbose(() => $"[Managed Identity] File path is valid. Path: {splitChallenge[1]}");

            var length = new FileInfo(splitChallenge[1]).Length;

            if ((!File.Exists(splitChallenge[1]) || (length) > 4096))
            {
                _requestContext.Logger.Error($"[Managed Identity] File does not exist or is greater than 4096 bytes. File exists: {File.Exists(splitChallenge[1])}. Length of file: {length}");
                throw CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    MsalErrorMessage.ManagedIdentityInvalidFile);
            }

            _requestContext.Logger.Verbose(() => "[Managed Identity] File exists and is less than 4096 bytes.");
        }

        private MsalException CreateManagedIdentityException(string errorCode, string errorMessage)
        {
            return MsalServiceExceptionFactory.CreateManagedIdentityException(
                errorCode,
                errorMessage,
                null,
                ManagedIdentitySource.AzureArc,
                null);
        }

        private bool IsValidPath(string path)
        {
            string expectedFilePath;

            if (DesktopOsHelper.IsWindows())
            {
                string expandedExpectedPath = Environment.ExpandEnvironmentVariables("%ProgramData%\\AzureConnectedMachineAgent\\Tokens\\");

                expectedFilePath = expandedExpectedPath + Path.GetFileNameWithoutExtension(path) + ".key";
            }
            else if (DesktopOsHelper.IsLinux())
            {
                expectedFilePath = "/var/opt/azcmagent/tokens/" + Path.GetFileNameWithoutExtension(path) + ".key";
            }
            else
            {
                throw CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    MsalErrorMessage.ManagedIdentityPlatformNotSupported);
            }

            return path.Equals(expectedFilePath);
        }
    }
}
