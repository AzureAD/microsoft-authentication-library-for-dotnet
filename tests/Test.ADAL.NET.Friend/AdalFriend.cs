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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.NET.Friend
{
    public static class AdalFriend
    {
        public static ClientAssertion CreateJwt(X509Certificate2 cert, string password, string issuer, string aud)
        {
            ClientAssertionCertificate certificate = new ClientAssertionCertificate(issuer, cert);
            JsonWebToken jwtToken = new JsonWebToken(certificate, aud);
            return jwtToken.Sign(certificate);
        }

        public async static Task<string> AcquireAccessCodeAsync(AuthenticationContext context, string resource, string clientId, Uri redirectUri, UserIdentifier userId, bool extendedLifeTimeEnabled)
        {
            HandlerData handlerData = new HandlerData(context.Authenticator,resource, clientId, extendedLifeTimeEnabled);
            var handler = new AcquireTokenInteractiveHandler(handlerData, redirectUri, new PlatformParameters(PromptBehavior.Auto, null), userId, null,
                context.CreateWebAuthenticationDialog(new PlatformParameters(PromptBehavior.Auto, null)));
            handler.CallState = null;
            context.Authenticator.AuthorizationUri = context.Authority + "oauth2/authorize";
            await handler.AcquireAuthorizationAsync();
            return handler.authorizationResult.Code;
        }

        public static void UpdateTokenExpiryOnTokenCache(TokenCache cache, DateTimeOffset newExpiry)
        {
            var cacheDictionary = cache.tokenCacheDictionary;

            var key = cacheDictionary.Keys.First();
            cache.tokenCacheDictionary[key].Result.ExpiresOn = newExpiry; 
            var value = cacheDictionary.Values.First();
            cache.Clear();
            cacheDictionary.Add(key, value);        
        }
    }
}
