// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.DownStreamRestApi;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Down stream api for public clients
    /// </summary>
    public class PublicClientDownstreamApi
    {
        string _resourceName;
        DownstreamRestApiOptions _options;
        IPublicClientApplication _pca;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="options"></param>
        public PublicClientDownstreamApi(string resourceName, DownstreamRestApiOptions options)
        {
            _resourceName = resourceName;
            _options = options;
            _pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(_options).Build();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetUrl"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> CallGetApiAsync(Uri targetUrl, CancellationToken cancellationToken = default)
        {
            WwwAuthenticateParameters parameters = await WwwAuthenticateParameters.CreateFromResourceResponseAsync(targetUrl.AbsoluteUri).ConfigureAwait(false);

            if (!parameters.IsBearerSupported && !parameters.IsPopSupported)
            {
                throw new MsalClientException("Neither bearer nor pop are supported");
            }

            var result = GetAuthResultAsync(parameters, targetUrl);

            // At this point, we have either a POP or a Bearer token

            // Make a call to the RP
            // If we get 200 OK -> update the nonce from the AuthenticateInfo Header
            // If we get 401 Unauthorized -> the nonce was probably expired, get the new one from the headers                
            HttpResponseMessage response = DownstreamRestApiHelper.TryGetResponseFromResourceAsync(targetUrl.AbsoluteUri, result);

            if (response.StatusCode = System.Net.HttpStatusCode.Unauthorized)
            {

            }

            if (newNonce != null)
                _currentNonce = newNonce;

            // old nonce probably expired, try again with newly acquired nonce
            if (response.HttpStatusCode == HttpStatus.Unauthorized)
            {
                // TODO: more error handling and give up after a few failed attempts to not freeze apps.
                CallGetApi(targetUrl, scopes);
            }

            return response;

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetUrl"></param>
        /// <param name="content"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> CallPostApiAsync(Uri targetUrl, FormUrlEncodedContent content, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        // Returns an AuthenticationResult for either Bearer or POP.
        private async Task<AuthenticationResult> GetAuthResultAsync(WwwAuthenticateParameters parameters, Uri targetUrl)
        {
            try
            {
                var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);

                var atsRequestBuilder = _pca.AcquireTokenSilent(_options.Scopes, accounts.FirstOrDefault());

                if (parameters.IsPopSupported)
                {
                    atsRequestBuilder = atsRequestBuilder.WithProofOfPossession(parameters.ServerNonce, HttpMethod.Get, targetUrl);
                }

                return await atsRequestBuilder.ExecuteAsync().ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                var atiRequestBuilder = _pca.AcquireTokenInteractive(_options.Scopes);

                if (parameters.IsPopSupported)
                {
                    atiRequestBuilder = atiRequestBuilder.WithProofOfPossession(parameters.ServerNonce, HttpMethod.Get, targetUrl);
                }

                return await atiRequestBuilder.ExecuteAsync().ConfigureAwait(false);
            }
        }
    }
}
