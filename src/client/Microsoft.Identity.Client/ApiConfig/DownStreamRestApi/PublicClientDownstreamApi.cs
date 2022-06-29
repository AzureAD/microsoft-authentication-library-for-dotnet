// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

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
        private string _currentNonce = null;

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
            WwwAuthenticateParameters parameters = null;
            try
            {
                parameters = await WwwAuthenticateParameters.CreateFromResourceResponseAsync(targetUrl.AbsoluteUri).ConfigureAwait(false);
            }
            catch(MsalClientException ex)
            {
                throw;
            }

            if (!parameters.IsBearerSupported && !parameters.IsPopSupported)
            {
                throw new MsalClientException("Neither bearer nor pop are supported");
            }

            _currentNonce = parameters.ServerNonce;

            var result = await GetAuthResultAsync(parameters.IsPopSupported, targetUrl, _currentNonce).ConfigureAwait(false);

            // At this point, we have either a POP or a Bearer token

            // Make a call to the RP
            // If we get 200 OK -> update the nonce from the AuthenticateInfo Header
            // If we get 401 Unauthorized -> the nonce was probably expired, get the new one from the headers                
            HttpResponseMessage response = await TryGetResponseFromResourceAsync(targetUrl.AbsoluteUri, result).ConfigureAwait(false);

            // old nonce probably expired, try again with newly acquired nonce
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // TODO: more error handling and give up after a few failed attempts to not freeze apps.
                await CallGetApiAsync(targetUrl, cancellationToken).ConfigureAwait(false);
            }

            return response;
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
        private async Task<AuthenticationResult> GetAuthResultAsync(bool isPopSupported, Uri targetUrl, string serverNonce)
        {
            try
            {
                var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);

                var atsRequestBuilder = _pca.AcquireTokenSilent(_options.Scopes, accounts.FirstOrDefault());

                if (isPopSupported)
                {
                    atsRequestBuilder = atsRequestBuilder.WithProofOfPossession(serverNonce, HttpMethod.Get, targetUrl);
                }

                return await atsRequestBuilder.ExecuteAsync().ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                var atiRequestBuilder = _pca.AcquireTokenInteractive(_options.Scopes);

                if (isPopSupported)
                {
                    atiRequestBuilder = atiRequestBuilder.WithProofOfPossession(serverNonce, HttpMethod.Get, targetUrl);
                }

                return await atiRequestBuilder.ExecuteAsync().ConfigureAwait(false);
            }
        }

        private async Task<HttpResponseMessage> TryGetResponseFromResourceAsync(string endpoint, AuthenticationResult authResult)
        {
            HttpClient client = new HttpClient();
            bool usingPop = authResult.TokenType == "pop";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue(authResult.TokenType, authResult.AccessToken);
            HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (usingPop)
                {
                    _currentNonce = ParseAuthHeaderForNextNonce(response.Headers);
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var parameters = WwwAuthenticateParameters.CreateFromResponseHeaders(response.Headers, Constants.PoPAuthHeaderPrefix);

                if (!parameters.IsPopSupported)
                {
                    throw new MsalClientException("RP no longer supports POP tokens. Please try authenticating again");
                }

                _currentNonce = parameters.ServerNonce;
            }

            return response;
        }

        private string ParseAuthHeaderForNextNonce(HttpResponseHeaders headers)
        {
            var authInfoHeaders = headers.Where(x => x.Key == "Authentication-Info");

            if (authInfoHeaders != null)
            {
                var authInfoHeader = authInfoHeaders.FirstOrDefault().Value;

                var PopHeaders = headers.Where(x => x.Key == Constants.PoPAuthHeaderPrefix);

                if (PopHeaders != null)
                {
                    return PopHeaders.FirstOrDefault().Value.ToString();
                }
            }
            return null;
        }
    }
}
