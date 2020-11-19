// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.UI;
#if ANDROID
using MsalAndroid = Com.Microsoft.Identity.Client;
#endif

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
#if ANDROID
    internal class AndroidAuthCallback : global::Java.Lang.Object, MsalAndroid.IAuthenticationCallback
    {
        MsalAndroid.IAuthenticationResult Result { get; set; }

        public void OnCancel()
        {
            throw new NotImplementedException();
        }

        public void OnError(MsalAndroid.Exception.MsalException p0)
        {
            throw new NotImplementedException();
        }

        public void OnSuccess(MsalAndroid.IAuthenticationResult result)
        {
            Result = result;
        }
    }
#endif

    internal class PublicClientExecutor : AbstractExecutor, IPublicClientApplicationExecutor
    {
        private readonly PublicClientApplication _publicClientApplication;
#if ANDROID
        private readonly MsalAndroid.IPublicClientApplication _boundApplication;

        public PublicClientExecutor(IServiceBundle serviceBundle, PublicClientApplication publicClientApplication, MsalAndroid.IPublicClientApplication boundApplication = null)
    : base(serviceBundle, publicClientApplication)
        {
            _publicClientApplication = publicClientApplication;
            _boundApplication = boundApplication;
        }
#endif

        public PublicClientExecutor(IServiceBundle serviceBundle, PublicClientApplication publicClientApplication)
            : base(serviceBundle, publicClientApplication)
        {
            _publicClientApplication = publicClientApplication;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenInteractiveParameters interactiveParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId);

#if ANDROID

            AndroidAuthCallback callback = new AndroidAuthCallback();

            var builder = new MsalAndroid.AcquireTokenParameters.Builder();
            builder.StartAuthorizationFromActivity(interactiveParameters.UiParent.Activity)
                .WithCallback(callback)
                .WithScopes(commonParameters.Scopes.ToList());

            MsalAndroid.AcquireTokenParameters parameters = builder.Build() as MsalAndroid.AcquireTokenParameters;
            _boundApplication.AcquireToken(parameters);

            return null;
#else

            AuthenticationRequestParameters requestParams = _publicClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _publicClientApplication.UserTokenCacheInternal);

            requestParams.LoginHint = interactiveParameters.LoginHint;
            requestParams.Account = interactiveParameters.Account;

            InteractiveRequest interactiveRequest = 
                new InteractiveRequest(requestParams, interactiveParameters);

            return await interactiveRequest.RunAsync(cancellationToken).ConfigureAwait(false);
#endif
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenWithDeviceCodeParameters deviceCodeParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId);

            var requestParams = _publicClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _publicClientApplication.UserTokenCacheInternal);

            var handler = new DeviceCodeRequest(
                ServiceBundle,
                requestParams,
                deviceCodeParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByIntegratedWindowsAuthParameters integratedWindowsAuthParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId);

            var requestParams = _publicClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _publicClientApplication.UserTokenCacheInternal);

            var handler = new IntegratedWindowsAuthRequest(
                ServiceBundle,
                requestParams,
                integratedWindowsAuthParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByUsernamePasswordParameters usernamePasswordParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId);

            var requestParams = _publicClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _publicClientApplication.UserTokenCacheInternal);

            var handler = new UsernamePasswordRequest(
                ServiceBundle,
                requestParams,
                usernamePasswordParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }


    }
}
