// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
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
            else if (string.IsNullOrWhiteSpace(clientCredential.Assertion))
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
                if (!string.IsNullOrEmpty(clientCredential.Secret))
                {
                    parameters[OAuth2Parameter.ClientSecret] = clientCredential.Secret;
                }
                else
                {
                    if ((clientCredential.Assertion == null || clientCredential.ValidTo != 0) && String.IsNullOrEmpty(clientCredential.SignedAssertion))
                    {
                        if (!ValidateClientAssertion(clientCredential, endpoints, sendX5C))
                        {
                            logger.Info("Client Assertion does not exist or near expiry.");
                            JsonWebToken jwtToken;
                            
                            if (clientCredential.UserProvidedClientAssertion != null)
                            {
                                jwtToken = new JsonWebToken(cryptographyManager, clientId, endpoints?.SelfSignedJwtAudience, clientCredential.UserProvidedClientAssertion);
                            }
                            else
                            {
                                jwtToken = new JsonWebToken(cryptographyManager, clientId, endpoints?.SelfSignedJwtAudience);
                            }
                            clientCredential.Assertion = jwtToken.Sign(clientCredential.Certificate, sendX5C);
                            clientCredential.ValidTo = jwtToken.ValidTo;
                            clientCredential.ContainsX5C = sendX5C;
                            clientCredential.Audience = endpoints?.SelfSignedJwtAudience;
                        }
                        else
                        {
                            logger.Info("Reusing the unexpired Client Assertion...");
                        }
                    }

                    parameters[OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer;
                    
                    if (!String.IsNullOrEmpty(clientCredential.SignedAssertion))
                    {
                        parameters[OAuth2Parameter.ClientAssertion] = clientCredential.SignedAssertion;
                    }
                    else
                    {
                        parameters[OAuth2Parameter.ClientAssertion] = clientCredential.Assertion;
                    }
                }
            }
            return parameters;
        }
#endif
    }
}
