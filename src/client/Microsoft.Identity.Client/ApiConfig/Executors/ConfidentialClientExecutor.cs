// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    internal class ConfidentialClientExecutor : AbstractExecutor, IConfidentialClientApplicationExecutor
    {
        private readonly ConfidentialClientApplication _confidentialClientApplication;

        public ConfidentialClientExecutor(IServiceBundle serviceBundle, ConfidentialClientApplication confidentialClientApplication)
            : base(serviceBundle)
        {
            ClientApplicationBase.GuardMobileFrameworks();

            _confidentialClientApplication = confidentialClientApplication;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByAuthorizationCodeParameters authorizationCodeParameters,
            CancellationToken cancellationToken)
        {
            RequestContext requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            AuthenticationRequestParameters requestParams = await _confidentialClientApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _confidentialClientApplication.UserTokenCacheInternal).ConfigureAwait(false);
            requestParams.SendX5C = authorizationCodeParameters.SendX5C ?? false;

            var handler = new ConfidentialAuthCodeRequest(
                ServiceBundle,
                requestParams,
                authorizationCodeParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenForClientParameters clientParameters,
            CancellationToken cancellationToken)
        {
            RequestContext requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            // Perform MTLS PoP validations if required
            ValidateAndConfigureMtlsPopForAcquireTokenForClient(clientParameters, commonParameters, requestContext);

            AuthenticationRequestParameters requestParams = await _confidentialClientApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _confidentialClientApplication.AppTokenCacheInternal).ConfigureAwait(false);
       
            requestParams.SendX5C = clientParameters.SendX5C ?? false;

            var handler = new ClientCredentialRequest(
                ServiceBundle,
                requestParams,
                clientParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        private void ValidateAndConfigureMtlsPopForAcquireTokenForClient(
            AcquireTokenForClientParameters clientParameters,
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext)
        {
            if (clientParameters.UseMtlsPop)
            {
                // Validate that the certificate is not null for MTLS PoP
                if (_confidentialClientApplication.Certificate == null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                // Validate that a region is specified when MTLS PoP is enabled
                bool isRegionMissing = string.IsNullOrEmpty(requestContext.ServiceBundle.Config.AzureRegion);

                if (isRegionMissing)
                {
                    throw new MsalClientException(
                        MsalError.MtlsPopWithoutRegion,
                        MsalErrorMessage.MtlsPopWithoutRegion);
                }

                commonParameters.MtlsCertificate = _confidentialClientApplication.Certificate;
                commonParameters.AuthenticationOperation = new MtlsPopAuthenticationOperation(_confidentialClientApplication.Certificate);
                ServiceBundle.Config.IsInstanceDiscoveryEnabled = false;
                ServiceBundle.Config.ClientCredential = null;              
                requestContext.UseMtlsPop = true;
            }
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenOnBehalfOfParameters onBehalfOfParameters,
            CancellationToken cancellationToken)
        {
            RequestContext requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            AuthenticationRequestParameters requestParams = await _confidentialClientApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _confidentialClientApplication.UserTokenCacheInternal).ConfigureAwait(false);

            requestParams.SendX5C = onBehalfOfParameters.SendX5C ?? false;
            requestParams.UserAssertion = onBehalfOfParameters.UserAssertion;
            requestParams.LongRunningOboCacheKey = onBehalfOfParameters.LongRunningOboCacheKey;

            var handler = new OnBehalfOfRequest(
                ServiceBundle,
                requestParams,
                onBehalfOfParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<Uri> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            GetAuthorizationRequestUrlParameters authorizationRequestUrlParameters,
            CancellationToken cancellationToken)
        {
            RequestContext requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            AuthenticationRequestParameters requestParameters = await _confidentialClientApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _confidentialClientApplication.UserTokenCacheInternal).ConfigureAwait(false);

            requestParameters.Account = authorizationRequestUrlParameters.Account;
            requestParameters.LoginHint = authorizationRequestUrlParameters.LoginHint;
            requestParameters.CcsRoutingHint = authorizationRequestUrlParameters.CcsRoutingHint;

            if (!string.IsNullOrWhiteSpace(authorizationRequestUrlParameters.RedirectUri))
            {
                requestParameters.RedirectUri = new Uri(authorizationRequestUrlParameters.RedirectUri);
            }

            await requestParameters.AuthorityManager.RunInstanceDiscoveryAndValidationAsync().ConfigureAwait(false);
            var handler = new AuthCodeRequestComponent(
                requestParameters,
                authorizationRequestUrlParameters.ToInteractiveParameters());

            if (authorizationRequestUrlParameters.CodeVerifier != null)
            {
                return await handler.GetAuthorizationUriWithPkceAsync(authorizationRequestUrlParameters.CodeVerifier, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await handler.GetAuthorizationUriWithoutPkceAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByUsernamePasswordParameters userNamePasswordParameters,
            CancellationToken cancellationToken)
        {
            RequestContext requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            AuthenticationRequestParameters requestParams = await _confidentialClientApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _confidentialClientApplication.UserTokenCacheInternal).ConfigureAwait(false);
            
            requestParams.SendX5C = userNamePasswordParameters.SendX5C ?? false;

            var handler = new UsernamePasswordRequest(
                ServiceBundle,
                requestParams,
                userNamePasswordParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
