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
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [DataContract]
    internal class TokenResponse
    {
        private const string CorrelationIdClaim = "correlation_id";

        [DataMember(Name = OAuthReservedClaim.TokenType, IsRequired = false)]
        public string TokenType { get; set; }

        [DataMember(Name = OAuthReservedClaim.AccessToken, IsRequired = false)]
        public string AccessToken { get; set; }

        [DataMember(Name = OAuthReservedClaim.RefreshToken, IsRequired = false)]
        public string RefreshToken { get; set; }

        [DataMember(Name = OAuthReservedClaim.Resource, IsRequired = false)]
        public string Resource { get; set; }

        [DataMember(Name = OAuthReservedClaim.IdToken, IsRequired = false)]
        public string IdToken { get; set; }

        [DataMember(Name = OAuthReservedClaim.CreatedOn, IsRequired = false)]
        public long CreatedOn { get; set; }

        [DataMember(Name = OAuthReservedClaim.ExpiresOn, IsRequired = false)]
        public long ExpiresOn { get; set; }

        [DataMember(Name = OAuthReservedClaim.ExpiresIn, IsRequired = false)]
        public long ExpiresIn { get; set; }

        [DataMember(Name = OAuthReservedClaim.Error, IsRequired = false)]
        public string Error { get; set; }

        [DataMember(Name = OAuthReservedClaim.ErrorDescription, IsRequired = false)]
        public string ErrorDescription { get; set; }

        [DataMember(Name = OAuthReservedClaim.ErrorCodes, IsRequired = false)]
        public string[] ErrorCodes { get; set; }

        [DataMember(Name = CorrelationIdClaim, IsRequired = false)]
        public string CorrelationId { get; set; }
    }

    [DataContract]
    internal class IdToken
    {
        [DataMember(Name = IdTokenClaim.ObjectId, IsRequired = false)]
        public string ObjectId { get; set; }

        [DataMember(Name = IdTokenClaim.Subject, IsRequired = false)]
        public string Subject { get; set; }

        [DataMember(Name = IdTokenClaim.TenantId, IsRequired = false)]
        public string TenantId { get; set; }

        [DataMember(Name = IdTokenClaim.UPN, IsRequired = false)]
        public string UPN { get; set; }

        [DataMember(Name = IdTokenClaim.GivenName, IsRequired = false)]
        public string GivenName { get; set; }

        [DataMember(Name = IdTokenClaim.FamilyName, IsRequired = false)]
        public string FamilyName { get; set; }

        [DataMember(Name = IdTokenClaim.Email, IsRequired = false)]
        public string Email { get; set; }

        [DataMember(Name = IdTokenClaim.PasswordExpiration, IsRequired = false)]
        public long PasswordExpiration { get; set; }

        [DataMember(Name = IdTokenClaim.PasswordChangeUrl, IsRequired = false)]
        public string PasswordChangeUrl { get; set; }

        [DataMember(Name = IdTokenClaim.IdentityProvider, IsRequired = false)]
        public string IdentityProvider { get; set; }

        [DataMember(Name = IdTokenClaim.Issuer, IsRequired = false)]
        public string Issuer { get; set; }
    }

    internal static class OAuth2Response
    {
        public static AuthenticationResult ParseTokenResponse(TokenResponse tokenResponse, CallState callState)
        {
            AuthenticationResult result;

            if (tokenResponse.AccessToken != null)
            {
                DateTimeOffset expiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResponse.ExpiresIn);

                result = new AuthenticationResult(tokenResponse.TokenType, tokenResponse.AccessToken, tokenResponse.RefreshToken, expiresOn)
                    {
#if ADAL_NET
                        // This is only needed for AcquireTokenByAuthorizationCode in which parameter resource is optional and we need
                        // to get it from the STS response.
                        Resource = tokenResponse.Resource,
#endif                        
                    };

                IdToken idToken = ParseIdToken(tokenResponse.IdToken);
                if (idToken != null)
                {
                    string tenantId = idToken.TenantId;
                    string uniqueId = null;
                    string displayableId = null;

                    if (!string.IsNullOrWhiteSpace(idToken.ObjectId))
                    {
                        uniqueId = idToken.ObjectId;
                    }
                    else if (!string.IsNullOrWhiteSpace(idToken.Subject))
                    {
                        uniqueId = idToken.Subject;
                    }

                    if (!string.IsNullOrWhiteSpace(idToken.UPN))
                    {
                        displayableId = idToken.UPN;
                    }
                    else if (!string.IsNullOrWhiteSpace(idToken.Email))
                    {
                        displayableId = idToken.Email;
                    }

                    string givenName = idToken.GivenName;
                    string familyName = idToken.FamilyName;
                    string identityProvider = idToken.IdentityProvider ?? idToken.Issuer;
                    DateTimeOffset? passwordExpiresOffest = null;
                    if (idToken.PasswordExpiration > 0)
                    {
                        passwordExpiresOffest = DateTime.UtcNow + TimeSpan.FromSeconds(idToken.PasswordExpiration);
                    }

                    Uri changePasswordUri = null;
                    if (!string.IsNullOrEmpty(idToken.PasswordChangeUrl))
                    {
                        changePasswordUri = new Uri(idToken.PasswordChangeUrl);
                    }

                    result.UpdateTenantAndUserInfo(tenantId, tokenResponse.IdToken, new UserInfo { UniqueId = uniqueId, DisplayableId = displayableId, GivenName = givenName, FamilyName = familyName, IdentityProvider = identityProvider, PasswordExpiresOn = passwordExpiresOffest, PasswordChangeUrl = changePasswordUri });
                }
            }
            else if (tokenResponse.Error != null)
            {
                throw new AdalServiceException(tokenResponse.Error, tokenResponse.ErrorDescription);
            }
            else
            {
                throw new AdalServiceException(AdalError.Unknown, AdalErrorMessage.Unknown);
            }

            return result;
        }

        public static AuthorizationResult ParseAuthorizeResponse(string webAuthenticationResult, CallState callState)
        {
            AuthorizationResult result = null;

            var resultUri = new Uri(webAuthenticationResult);

            // NOTE: The Fragment property actually contains the leading '#' character and that must be dropped
            string resultData = resultUri.Query;

            if (!string.IsNullOrWhiteSpace(resultData))
            {
                // Remove the leading '?' first
                Dictionary<string, string> response = EncodingHelper.ParseKeyValueList(resultData.Substring(1), '&', true, callState);

                if (response.ContainsKey(OAuthReservedClaim.Code))
                {
                    result = new AuthorizationResult(response[OAuthReservedClaim.Code]);
                }
                else if (response.ContainsKey(OAuthReservedClaim.Error))
                {
                    result = new AuthorizationResult(response[OAuthReservedClaim.Error], response.ContainsKey(OAuthReservedClaim.ErrorDescription) ? response[OAuthReservedClaim.ErrorDescription] : null);
                }
                else
                {
                    result = new AuthorizationResult(AdalError.AuthenticationFailed, AdalErrorMessage.AuthorizationServerInvalidResponse);
                }
            }

            return result;
        }

        public static TokenResponse ReadErrorResponse(WebResponse response)
        {
            if (response == null)
            {
                return new TokenResponse 
                    { 
                        Error = AdalError.ServiceReturnedError,
                        ErrorDescription = AdalErrorMessage.ServiceReturnedError 
                    };
            }

            Stream responseStream = response.GetResponseStream();

            if (responseStream == null)
            {
                return new TokenResponse 
                    { 
                        Error = AdalError.Unknown, 
                        ErrorDescription = AdalErrorMessage.Unknown 
                    };
            }

            TokenResponse tokenResponse;
            StringBuilder responseStreamString = new StringBuilder();
            try
            {
                responseStreamString.Append(HttpHelper.ReadStreamContent(responseStream));
                using (MemoryStream ms = new MemoryStream(responseStreamString.ToByteArray()))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof (TokenResponse));
                    tokenResponse = ((TokenResponse) serializer.ReadObject(ms));
                }
            }
            catch (SerializationException)
            {
                tokenResponse = new TokenResponse
                {
                    Error = (((HttpWebResponse)response).StatusCode == HttpStatusCode.ServiceUnavailable) ? 
                        AdalError.ServiceUnavailable : 
                        AdalError.Unknown,
                    ErrorDescription = responseStreamString.ToString()
                };
            }

            return tokenResponse;
        }

        private static IdToken ParseIdToken(string idToken)
        {
            IdToken idTokenBody = null;
            if (!string.IsNullOrWhiteSpace(idToken))
            {
                string[] idTokenSegments = idToken.Split(new[] { '.' });

                // If Id token format is invalid, we silently ignore the id token
                if (idTokenSegments.Length == 3)    
                {
                    try
                    {
                        byte[] idTokenBytes = Base64UrlEncoder.DecodeBytes(idTokenSegments[1]);
                        using (var stream = new MemoryStream(idTokenBytes))
                        {
                            var serializer = new DataContractJsonSerializer(typeof(IdToken));
                            idTokenBody = (IdToken)serializer.ReadObject(stream);
                        }
                    }
                    catch (SerializationException)
                    {
                        // We silently ignore the id token if exception occurs.   
                    }
                    catch (ArgumentException)
                    {
                        // Again, we silently ignore the id token if exception occurs.   
                    }
                }
            }

            return idTokenBody;
        }
    }
}
