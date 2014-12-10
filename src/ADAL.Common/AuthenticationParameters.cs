//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Contains authentication parameters based on unauthorized response from resource server.
    /// </summary>
    public sealed partial class AuthenticationParameters
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
                Logger.Error(null, ex);
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
            CallState callState = new CallState(Guid.NewGuid(), false);

            if (resourceUrl == null)
            {
                throw new ArgumentNullException("resourceUrl");
            }

            IHttpWebResponse response = null;
            AuthenticationParameters authParams;

            try
            {
                IHttpWebRequest request = NetworkPlugin.HttpWebRequestFactory.Create(resourceUrl.AbsoluteUri);
                request.ContentType = "application/x-www-form-urlencoded";
                response = await request.GetResponseSyncOrAsync(callState);
                var ex = new AdalException(AdalError.UnauthorizedResponseExpected);
                Logger.Error(null, ex);
                throw ex;
            }
            catch (WebException ex)
            {
                response = NetworkPlugin.HttpWebRequestFactory.CreateResponse(ex.Response);
                if (response == null)
                {
                    var serviceEx = new AdalServiceException(AdalErrorMessage.UnauthorizedHttpStatusCodeExpected, ex);
                    Logger.Error(null, serviceEx);
                    throw serviceEx;
                }

                authParams = CreateFromUnauthorizedResponseCommon(response);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
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
                if (response.Headers.AllKeys.Contains(AuthenticateHeader))
                {
                    authParams = CreateFromResponseAuthenticateHeader(response.Headers[AuthenticateHeader]);
                }
                else
                {
                    var ex = new ArgumentException(AdalErrorMessage.MissingAuthenticateHeader, "response");
                    Logger.Error(null, ex);
                    throw ex;
                }
            }
            else
            {
                var ex = new ArgumentException(AdalErrorMessage.UnauthorizedHttpStatusCodeExpected, "response");
                Logger.Error(null, ex);
                throw ex;
            }

            return authParams;
        }
    }
}