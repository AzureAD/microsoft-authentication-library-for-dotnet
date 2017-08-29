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
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Extension class to support confidential client flows.
    /// </summary>
    public static class AuthenticationContextConfidentialClientExtensions
    {

#if !WINDOWS_APP
        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public static async Task<AuthenticationResult> AcquireTokenSilentAsync(this AuthenticationContext ctx, string resource,
            ClientCredential clientCredential, UserIdentifier userId)
        {
            return await ctx.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientCredential), userId, null)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public static async Task<AuthenticationResult> AcquireTokenSilentAsync(this AuthenticationContext ctx, string resource,
            IClientAssertionCertificate clientCertificate, UserIdentifier userId)
        {
            return await ctx.AcquireTokenSilentCommonAsync(resource,
                new ClientKey(clientCertificate, ctx.Authenticator), userId, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public static async Task<AuthenticationResult> AcquireTokenSilentAsync(this AuthenticationContext ctx, string resource,
            ClientAssertion clientAssertion, UserIdentifier userId)
        {
            return await ctx.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientAssertion), userId, null)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="clientCredential">The credential to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public static async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(this AuthenticationContext ctx, string authorizationCode,
            Uri redirectUri, ClientCredential clientCredential)
        {
            return await ctx
                .AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                    new ClientKey(clientCredential), null).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="clientCredential">The credential to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public static async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(this AuthenticationContext ctx, string authorizationCode,
            Uri redirectUri, ClientCredential clientCredential, string resource)
        {
            return await ctx
                .AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                    new ClientKey(clientCredential), resource).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public static async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(this AuthenticationContext ctx, string authorizationCode,
            Uri redirectUri, ClientAssertion clientAssertion)
        {
            return await ctx
                .AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                    new ClientKey(clientAssertion), null).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public static async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(this AuthenticationContext ctx, string authorizationCode,
            Uri redirectUri, ClientAssertion clientAssertion, string resource)
        {
            return await ctx
                .AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                    new ClientKey(clientAssertion), resource).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public static async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(this AuthenticationContext ctx, string authorizationCode,
            Uri redirectUri, IClientAssertionCertificate clientCertificate)
        {
            return await ctx.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                new ClientKey(clientCertificate, ctx.Authenticator), null).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public static async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(this AuthenticationContext ctx, string authorizationCode,
            Uri redirectUri, IClientAssertionCertificate clientCertificate, string resource)
        {
            return await ctx.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                new ClientKey(clientCertificate, ctx.Authenticator), resource).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public static async Task<AuthenticationResult> AcquireTokenAsync(this AuthenticationContext ctx, string resource, ClientCredential clientCredential,
            UserAssertion userAssertion)
        {
            return await ctx.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCredential), userAssertion)
                .ConfigureAwait(false);
        }


        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public static async Task<AuthenticationResult> AcquireTokenAsync(this AuthenticationContext ctx, string resource,
            IClientAssertionCertificate clientCertificate, UserAssertion userAssertion)
        {
            return await ctx
                .AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCertificate, ctx.Authenticator),
                    userAssertion).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public static async Task<AuthenticationResult> AcquireTokenAsync(this AuthenticationContext ctx, string resource, ClientAssertion clientAssertion,
            UserAssertion userAssertion)
        {
            return await ctx.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientAssertion), userAssertion)
                .ConfigureAwait(false);
        }


        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userAssertion">The assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public static async Task<AuthenticationResult> AcquireTokenAsync(this AuthenticationContext ctx, string resource, string clientId,
            UserAssertion userAssertion)
        {
            return await ctx.AcquireTokenCommonAsync(resource, clientId, userAssertion).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public static async Task<AuthenticationResult> AcquireTokenAsync(this AuthenticationContext ctx, string resource,
            IClientAssertionCertificate clientCertificate)
        {
            return await ctx
                .AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCertificate, ctx.Authenticator))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public static async Task<AuthenticationResult> AcquireTokenAsync(this AuthenticationContext ctx, string resource, ClientAssertion clientAssertion)
        {
            return await ctx.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientAssertion))
                .ConfigureAwait(false);
        }
#endif

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="ctx">Authentication context instance</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public static async Task<AuthenticationResult> AcquireTokenAsync(this AuthenticationContext ctx, string resource, ClientCredential clientCredential)
        {
            return await ctx.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCredential))
                .ConfigureAwait(false);
        }
    }
}
