//------------------------------------------------------------------------------
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
        public const string Scope = "scope";
        public const string FamilyOfClientId = "TO_BE_DECIDED";
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

        [DataMember(Name = TokenResponseClaim.Scope, IsRequired = false)]
        public string Scope { get; set; }

        [DataMember(Name = TokenResponseClaim.FamilyOfClientId, IsRequired = false)]
        public string FamilyOfClientId { get; set; }

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

        internal static TokenResponse CreateFromBrokerResponse(IDictionary<string, string> responseDictionary)
        {
            if (responseDictionary.ContainsKey(TokenResponseClaim.ErrorDescription))
            {
                return new TokenResponse
                {
                    Error = responseDictionary[TokenResponseClaim.Error],
                    ErrorDescription = responseDictionary[TokenResponseClaim.ErrorDescription]
                };
            }
            else
            {
                return new TokenResponse
                {
                    AccessToken = responseDictionary["access_token"],
                    RefreshToken = responseDictionary["refresh_token"],
                    IdTokenString = responseDictionary["id_token"],
                    TokenType = "Bearer",
                    CorrelationId = responseDictionary["correlation_id"],
                    //TODO - Get scopes instead. Resource = responseDictionary["resource"],
                    ExpiresOn = long.Parse(responseDictionary["expires_on"].Split('.')[0])
                };
            }
        }

        public static TokenResponse CreateFromErrorResponse(IHttpWebResponse webResponse)
        {
            if (webResponse == null)
            {
                return new TokenResponse
                {
                    Error = MsalError.ServiceReturnedError,
                    ErrorDescription = MsalErrorMessage.ServiceReturnedError
                };
            }

            Stream responseStream = webResponse.ResponseStream;

            if (responseStream == null)
            {
                return new TokenResponse
                {
                    Error = MsalError.Unknown,
                    ErrorDescription = MsalErrorMessage.Unknown
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
                        MsalError.ServiceUnavailable :
                        MsalError.Unknown,
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

                    result.UpdateTenantAndUser(tenantId, this.IdTokenString, new User { UniqueId = uniqueId, DisplayableId = displayableId, GivenName = givenName, FamilyName = familyName, IdentityProvider = identityProvider, PasswordExpiresOn = passwordExpiresOffest, PasswordChangeUrl = changePasswordUri });
                }

                resultEx = new AuthenticationResultEx
                {
                    Result = result,
                    RefreshToken = this.RefreshToken,
                    // This is only needed for AcquireTokenByAuthorizationCode in which parameter resource is optional and we need
                    // to get it from the STS response.
                    ScopeInResponse = Scope.CreateArrayFromSingleString()
                };
            }
            else if (this.Error != null)
            {
                throw new MsalServiceException(this.Error, this.ErrorDescription);
            }
            else
            {
                throw new MsalServiceException(MsalError.Unknown, MsalErrorMessage.Unknown);
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
