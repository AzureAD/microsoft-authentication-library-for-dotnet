//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Contains authentication parameters based on unauthorized response from resource server.
    /// </summary>
    public sealed class AuthenticationParameters
    {
        private const string AuthenticateHeader = "WWW-Authenticate";
        private const string Bearer = "bearer";
        private const string AuthorityKey = "authorization_uri";
        private const string ResourceKey = "resource_id";

        /// <summary>
        /// Gets or sets the address of the authority to issue token.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the target resource that is the recipient of the requested token.
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Creates authentication parameters from address of the resource. This method expects the resource server to return unauthorized response
        /// with WWW-Authenticate header containing authentication parameters.
        /// </summary>
        /// <param name="resourceUrl">Address of the resource</param>
        /// <returns>AuthenticationParameters object containing authentication parameters</returns>
        public static async Task<AuthenticationParameters> CreateFromResourceUrlAsync(Uri resourceUrl)
        {
            return await CreateFromResourceUrlCommonAsync(resourceUrl).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates authentication parameters from the response received from the response received from the resource. This method expects the response to have unauthorized status and
        /// WWW-Authenticate header containing authentication parameters.</summary>
        /// <param name="responseMessage">Response received from the resource (e.g. via an http call using HttpClient).</param>
        /// <returns>AuthenticationParameters object containing authentication parameters</returns>

        public static async Task<AuthenticationParameters> CreateFromUnauthorizedResponseAsync(HttpResponseMessage responseMessage)
        {
            return CreateFromUnauthorizedResponseCommon(await HttpClientWrapper.CreateResponseAsync(responseMessage).ConfigureAwait(false));
        }

        /// <summary>
        /// Creates authentication parameters from the WWW-Authenticate header in response received from resource. This method expects the header to contain authentication parameters.
        /// </summary>
        /// <param name="authenticateHeader">Content of header WWW-Authenticate header</param>
        /// <returns>AuthenticationParameters object containing authentication parameters</returns>
        public static AuthenticationParameters CreateFromResponseAuthenticateHeader(string authenticateHeader)
        {
            if (string.IsNullOrWhiteSpace(authenticateHeader))
            {
                throw new ArgumentNullException("authenticateHeader");
            }

            authenticateHeader = authenticateHeader.Trim();

            // This also checks for cases like "BearerXXXX authorization_uri=...." and "Bearer" and "Bearer "
            if (!authenticateHeader.StartsWith(Bearer, StringComparison.OrdinalIgnoreCase)
                || authenticateHeader.Length < Bearer.Length + 2
                || !char.IsWhiteSpace(authenticateHeader[Bearer.Length]))
            {
                var ex = new ArgumentException(AdalErrorMessage.InvalidAuthenticateHeaderFormat, "authenticateHeader");
                PlatformPlugin.Logger.Error(null, ex);
                throw ex;
            }

            authenticateHeader = authenticateHeader.Substring(Bearer.Length).Trim();

            Dictionary<string, string> authenticateHeaderItems = EncodingHelper.ParseKeyValueList(authenticateHeader, ',', false, null);

            var authParams = new AuthenticationParameters();
            string param;
            authenticateHeaderItems.TryGetValue(AuthorityKey, out param);
            authParams.Authority = param;
            authenticateHeaderItems.TryGetValue(ResourceKey, out param);
            authParams.Resource = param;

            return authParams;
        }

        private static async Task<AuthenticationParameters> CreateFromResourceUrlCommonAsync(Uri resourceUrl)
        {
            if (resourceUrl == null)
            {
                throw new ArgumentNullException("resourceUrl");
            }

            AuthenticationParameters authParams;

            try
            {
                IHttpClient request = PlatformPlugin.HttpClientFactory.Create(resourceUrl.AbsoluteUri, null);
                using (await request.GetResponseAsync().ConfigureAwait(false))
                {
                    var ex = new AdalException(AdalError.UnauthorizedResponseExpected);
                    PlatformPlugin.Logger.Error(null, ex);
                    throw ex;                    
                }
            }
            catch (HttpRequestWrapperException ex)
            {
                IHttpWebResponse response = ex.WebResponse;
                if (response == null)
                {
                    var serviceEx = new AdalServiceException(AdalErrorMessage.UnauthorizedHttpStatusCodeExpected, ex);
                    PlatformPlugin.Logger.Error(null, serviceEx);
                    throw serviceEx;
                }

                authParams = CreateFromUnauthorizedResponseCommon(response);
            }

            return authParams;
        }

        private static AuthenticationParameters CreateFromUnauthorizedResponseCommon(IHttpWebResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            AuthenticationParameters authParams;
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (response.Headers.Keys.Contains(AuthenticateHeader))
                {
                    authParams = CreateFromResponseAuthenticateHeader(response.Headers[AuthenticateHeader]);
                }
                else
                {
                    var ex = new ArgumentException(AdalErrorMessage.MissingAuthenticateHeader, "response");
                    PlatformPlugin.Logger.Error(null, ex);
                    throw ex;
                }
            }
            else
            {
                var ex = new ArgumentException(AdalErrorMessage.UnauthorizedHttpStatusCodeExpected, "response");
                PlatformPlugin.Logger.Error(null, ex);
                throw ex;
            }

            return authParams;
        }
    }
}
