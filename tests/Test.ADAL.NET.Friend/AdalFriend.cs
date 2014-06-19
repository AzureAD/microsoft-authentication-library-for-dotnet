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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.NET.Friend
{
    public static class AdalFriend
    {
        public static ClientAssertion CreateJwt(X509Certificate2 cert, string issuer, string aud)
        {
            // Thirty minutes
            const uint JwtToAcsLifetimeInSeconds = 60 * 30; 

            X509CertificateCredential x509ClientCredential = new X509CertificateCredential(issuer, cert);
            JsonWebToken jwtToken = new JsonWebToken(aud, issuer, JwtToAcsLifetimeInSeconds, issuer);
            return jwtToken.Sign(x509ClientCredential);
        }

        public static string AcquireAccessCode(AuthenticationContext context, string resource, string clientId, Uri redirectUri, UserIdentifier userId)
        {
            context.CreateAuthenticatorAsync(null).Wait();
            AuthorizationResult authorizationResult = context.SendAuthorizeRequest(resource, clientId, redirectUri, userId, PromptBehavior.Auto, null, null);
            return authorizationResult.Code;
        }

        public static void UpdateTokenExpiryOnTokenCache(TokenCache cache, DateTimeOffset newExpiry)
        {
            var cacheStore = cache.TokenCacheStore;

            var key = cacheStore.Keys.First();
            key.ExpiresOn = newExpiry; 
            var value = cacheStore.Values.First();
            cache.Clear();
            cacheStore.Add(key, value);        
        }
    }
}
