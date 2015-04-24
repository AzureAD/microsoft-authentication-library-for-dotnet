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
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class TokenResponseClaim
    {
        public const string Code = "code";
        public const string TokenType = "token_type";
        public const string AccessToken = "access_token";
        public const string RefreshToken = "refresh_token";
        public const string Resource = "resource";
        public const string IdToken = "id_token";
        public const string CreatedOn = "created_on";
        public const string ExpiresOn = "expires_on";
        public const string ExpiresIn = "expires_in";
        public const string Error = "error";
        public const string ErrorDescription = "error_description";
        public const string ErrorCodes = "error_codes";
    }

    [DataContract]
    internal class TokenResponse
    {
        private const string CorrelationIdClaim = "correlation_id";

        [DataMember(Name = TokenResponseClaim.TokenType, IsRequired = false)]
        public string TokenType { get; set; }

        [DataMember(Name = TokenResponseClaim.AccessToken, IsRequired = false)]
        public string AccessToken { get; set; }

        [DataMember(Name = TokenResponseClaim.RefreshToken, IsRequired = false)]
        public string RefreshToken { get; set; }

        [DataMember(Name = TokenResponseClaim.Resource, IsRequired = false)]
        public string Resource { get; set; }

        [DataMember(Name = TokenResponseClaim.IdToken, IsRequired = false)]
        public string IdTokenString { get; set; }

        [DataMember(Name = TokenResponseClaim.CreatedOn, IsRequired = false)]
        public long CreatedOn { get; set; }

        [DataMember(Name = TokenResponseClaim.ExpiresOn, IsRequired = false)]
        public long ExpiresOn { get; set; }

        [DataMember(Name = TokenResponseClaim.ExpiresIn, IsRequired = false)]
        public long ExpiresIn { get; set; }

        [DataMember(Name = TokenResponseClaim.Error, IsRequired = false)]
        public string Error { get; set; }

        [DataMember(Name = TokenResponseClaim.ErrorDescription, IsRequired = false)]
        public string ErrorDescription { get; set; }

        [DataMember(Name = TokenResponseClaim.ErrorCodes, IsRequired = false)]
        public string[] ErrorCodes { get; set; }

        [DataMember(Name = CorrelationIdClaim, IsRequired = false)]
        public string CorrelationId { get; set; }

        public static TokenResponse CreateFromErrorResponse(IHttpWebResponse webResponse)
        {
            if (webResponse == null)
            {
                return new TokenResponse
                {
                    Error = AdalError.ServiceReturnedError,
                    ErrorDescription = AdalErrorMessage.ServiceReturnedError
                };
            }

            Stream responseStream = webResponse.ResponseStream;

            if (responseStream == null)
            {
                return new TokenResponse
                {
                    Error = AdalError.Unknown,
                    ErrorDescription = AdalErrorMessage.Unknown
                };
            }

            TokenResponse tokenResponse;

            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(TokenResponse));
                tokenResponse = ((TokenResponse)serializer.ReadObject(responseStream));

                // Reset stream position to make it possible for application to read HttpRequestException body again
                responseStream.Position = 0;
            }
            catch (SerializationException)
            {
                responseStream.Position = 0;
                tokenResponse = new TokenResponse
                {
                    Error = (webResponse.StatusCode == HttpStatusCode.ServiceUnavailable) ?
                        AdalError.ServiceUnavailable :
                        AdalError.Unknown,
                    ErrorDescription = ReadStreamContent(responseStream)
                };
            }

            return tokenResponse;
        }

        public AuthenticationResultEx GetResult()
        {
            AuthenticationResultEx resultEx;

            if (this.AccessToken != null)
            {
                DateTimeOffset expiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(this.ExpiresIn);

                var result = new AuthenticationResult(this.TokenType, this.AccessToken, expiresOn);

                IdToken idToken = IdToken.Parse(this.IdTokenString);
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

                    result.UpdateTenantAndUserInfo(tenantId, this.IdTokenString, new UserInfo { UniqueId = uniqueId, DisplayableId = displayableId, GivenName = givenName, FamilyName = familyName, IdentityProvider = identityProvider, PasswordExpiresOn = passwordExpiresOffest, PasswordChangeUrl = changePasswordUri });
                }

                resultEx = new AuthenticationResultEx
                {
                    Result = result,
                    RefreshToken = this.RefreshToken,
                    // This is only needed for AcquireTokenByAuthorizationCode in which parameter resource is optional and we need
                    // to get it from the STS response.
                    ResourceInResponse = this.Resource                    
                };
            }
            else if (this.Error != null)
            {
                throw new AdalServiceException(this.Error, this.ErrorDescription);
            }
            else
            {
                throw new AdalServiceException(AdalError.Unknown, AdalErrorMessage.Unknown);
            }

            return resultEx;
        }

        private static string ReadStreamContent(Stream stream)
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }
    }

}
