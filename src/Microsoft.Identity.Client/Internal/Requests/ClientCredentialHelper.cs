// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal static class ClientCredentialHelper
    {
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms
        /// <summary>
        ///     Determines whether or not the cached client assertion can be used again for the next authentication request by
        ///     checking it's
        ///     values against incoming request parameters.
        /// </summary>
        /// <returns>Returns true if the previously cached client assertion is valid</returns>
        public static bool ValidateClientAssertion(ClientCredentialWrapper clientCredential, AuthorityEndpoints endpoints, bool sendX5C)
        {
            if (clientCredential == null)
            {
                throw new ArgumentNullException(nameof(clientCredential));
            }
            else if (string.IsNullOrWhiteSpace(clientCredential.CachedAssertion))
            {
                return false;
            }

            //Check if all current client assertion values match incoming parameters and expiration time
            //The clientCredential object contains the previously used values in the cached client assertion string
            bool expired = clientCredential.ValidTo <=
                           JsonWebToken.ConvertToTimeT(
                               DateTime.UtcNow + TimeSpan.FromMinutes(Constants.ExpirationMarginInMinutes));

            bool parametersMatch = clientCredential.Audience == endpoints?.SelfSignedJwtAudience &&
                                   clientCredential.ContainsX5C == sendX5C;

            return !expired && parametersMatch;
        }

        public static Dictionary<string, string> CreateClientCredentialBodyParameters(
            ICoreLogger logger,
            ICryptographyManager cryptographyManager,
            ClientCredentialWrapper clientCredential,
            string clientId,
            AuthorityEndpoints endpoints,
            bool sendX5C)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (clientCredential != null)
            {
                if (clientCredential.AuthenticationType == ConfidentialClientAuthenticationType.ClientSecret)
                {
                    parameters[OAuth2Parameter.ClientSecret] = clientCredential.Secret;
                }
                else
                {
                    if ((clientCredential.CachedAssertion == null || clientCredential.ValidTo != 0) && clientCredential.AuthenticationType != ConfidentialClientAuthenticationType.SignedClientAssertion)
                    {
                        if (!ValidateClientAssertion(clientCredential, endpoints, sendX5C))
                        {
                            logger.Info(LogMessages.ClientAssertionDoesNotExistOrNearExpiry);
                           
                            JsonWebToken jwtToken;
                            
                            if (clientCredential.AuthenticationType == ConfidentialClientAuthenticationType.ClientCertificateWithClaims)
                            {
                                jwtToken = new JsonWebToken(cryptographyManager, clientId, endpoints?.SelfSignedJwtAudience, clientCredential.ClaimsToSign, clientCredential.AppendDefaultClaims);
                            }
                            else
                            {
                                jwtToken = new JsonWebToken(cryptographyManager, clientId, endpoints?.SelfSignedJwtAudience);
                            }

                            clientCredential.CachedAssertion = jwtToken.Sign(clientCredential, sendX5C);
                            clientCredential.ValidTo = jwtToken.ValidTo;
                            clientCredential.ContainsX5C = sendX5C;
                            clientCredential.Audience = endpoints?.SelfSignedJwtAudience;
                        }
                        else
                        {
                            logger.Info(LogMessages.ReusingTheUnexpiredClientAssertion);
                        }
                    }

                    parameters[OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer;
                    
                    if (clientCredential.AuthenticationType == ConfidentialClientAuthenticationType.SignedClientAssertion)
                    {
                        parameters[OAuth2Parameter.ClientAssertion] = clientCredential.SignedAssertion;
                    }
                    else
                    {
                        parameters[OAuth2Parameter.ClientAssertion] = clientCredential.CachedAssertion;
                    }
                }
            }
            return parameters;
        }
#endif
    }
}
