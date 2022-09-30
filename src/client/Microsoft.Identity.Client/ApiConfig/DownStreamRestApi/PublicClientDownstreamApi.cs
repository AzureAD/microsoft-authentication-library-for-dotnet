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
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Down stream api for public clients
    /// </summary>
    public class PublicClientDownstreamApi
    {
        private readonly int _maximumAuthenticationAttempts = 3;
        string _resourceName;
        DownstreamRestApiOptions _options;
        IPublicClientApplication _pca;
        private string _currentNonce = null;
        //TODO, grab identity logger from application.
        IIdentityLogger _logger = NullIdentityModelLogger.Instance;
        private int _currentAuthetnicatinonAttempts = 0;

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
            //_logger = options.logger.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetUrl"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> CallGetApiAsync(Uri targetUrl, CancellationToken cancellationToken = default)
        {
            bool isPopSupportedByResource = false;

            if (_pca.IsProofOfPossessionSupportedByClient())
            {
                AuthenticationHeaderParser parsedHeaders = null;
                try
                {
                    parsedHeaders = await AuthenticationHeaderParser.ParseAuthenticationHeadersAsync(targetUrl.AbsoluteUri).ConfigureAwait(false);
                }
                catch (MsalClientException)
                {
                    throw;
                }

                //Resource returned a pop header containing a nonce.
                isPopSupportedByResource = parsedHeaders.Nonce != null;

                if (!isPopSupportedByResource)
                {
                    _logger.Log(new LogEntry() { Message = "Proof-of-Possesion is not supported by resource." });
                }
                else
                {
                    _currentNonce = parsedHeaders.Nonce;
                }
            }

            var result = await GetAuthResultAsync(targetUrl, _currentNonce).ConfigureAwait(false);

            // At this point, we have either a POP or a Bearer token

            // Make a call to the RP
            // If we get 200 OK -> update the nonce from the AuthenticateInfo Header
            // If we get 401 Unauthorized -> the nonce was probably expired, get the new one from the headers                
            HttpResponseMessage response = await TryGetResponseFromResourceAsync(targetUrl.AbsoluteUri, result).ConfigureAwait(false);

            _currentAuthetnicatinonAttempts++;

            // old nonce probably expired, try again with newly acquired nonce
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (_currentAuthetnicatinonAttempts == _maximumAuthenticationAttempts)
                {
                    _currentAuthetnicatinonAttempts = 0;
                    throw new MsalClientException($"Failed to communicate with resource successfully. Responce reason: {response.ReasonPhrase}");
                }
                // TODO: more error handling
                await CallGetApiAsync(targetUrl, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        // Returns an AuthenticationResult for either Bearer or POP.
        private async Task<AuthenticationResult> GetAuthResultAsync(Uri targetUrl, string serverNonce)
        {
            bool isPopSupported = !string.IsNullOrEmpty(serverNonce);

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
                    _currentNonce = AuthenticationHeaderParser.ParseAuthenticationHeaders(response.Headers).Nonce;
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var parsedHeaders = AuthenticationHeaderParser.ParseAuthenticationHeaders(response.Headers);

                bool isPopSupportedByResource = parsedHeaders.Nonce != null;

                if (!isPopSupportedByResource)
                {
                    throw new MsalClientException("RP no longer supports POP tokens. Please try authenticating again.");
                }

                _currentNonce = parsedHeaders.Nonce;
            }

            return response;
        }
    }
}
