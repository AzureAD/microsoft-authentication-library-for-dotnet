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
using System.Text;
using Microsoft.Identity.Client.Interfaces;

namespace Microsoft.Identity.Client.Internal
{
    internal class TokenResponseClaim
    {
        public const string Code = "code";
        public const string TokenType = "token_type";
        public const string AccessToken = "access_token";
        public const string RefreshToken = "refresh_token";
        public const string Scope = "scope";
        public const string FamilyId = "foci";
        public const string IdToken = "id_token";
        public const string ExpiresIn = "expires_in";
        public const string Error = "error";
        public const string ErrorDescription = "error_description";
        public const string ErrorCodes = "error_codes";
        public const string CorrelationId = "correlation_id";
    }

    [DataContract]
    internal class TokenResponse
    {

        [DataMember(Name = TokenResponseClaim.TokenType, IsRequired = false)]
        public string TokenType { get; set; }

        [DataMember(Name = TokenResponseClaim.AccessToken, IsRequired = false)]
        public string AccessToken { get; set; }

        [DataMember(Name = TokenResponseClaim.RefreshToken, IsRequired = false)]
        public string RefreshToken { get; set; }

        [DataMember(Name = TokenResponseClaim.Scope, IsRequired = false)]
        public string Scope { get; set; }

        [DataMember(Name = TokenResponseClaim.FamilyId, IsRequired = false)]
        public string FamilyId { get; set; }

        [DataMember(Name = TokenResponseClaim.IdToken, IsRequired = false)]
        public string IdTokenString { get; set; }

        [DataMember(Name = TokenResponseClaim.ExpiresIn, IsRequired = false)]
        public long ExpiresIn { get; set; }

        [DataMember(Name = TokenResponseClaim.Error, IsRequired = false)]
        public string Error { get; set; }

        [DataMember(Name = TokenResponseClaim.ErrorDescription, IsRequired = false)]
        public string ErrorDescription { get; set; }

        [DataMember(Name = TokenResponseClaim.ErrorCodes, IsRequired = false)]
        public string[] ErrorCodes { get; set; }

        [DataMember(Name = TokenResponseClaim.CorrelationId, IsRequired = false)]
        public string CorrelationId { get; set; }

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
            StringBuilder responseStreamString = new StringBuilder();
            TokenResponse tokenResponse = null;
            using (Stream responseStream = webResponse.ResponseStream)
            {
                if (responseStream == null)
                {
                    return new TokenResponse
                    {
                        Error = MsalError.Unknown,
                        ErrorDescription = MsalErrorMessage.Unknown
                    };
                }

                try
                {
                    responseStreamString.Append(ReadStreamContent(responseStream));
                    using (MemoryStream ms = new MemoryStream(responseStreamString.ToByteArray()))
                    {
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(TokenResponse));
                        tokenResponse = ((TokenResponse)serializer.ReadObject(ms));
                    }
                }
                catch (SerializationException ex)
                {
                    PlatformPlugin.Logger.Warning(null, ex.Message);
                    tokenResponse = new TokenResponse
                    {
                        Error = (webResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
                            ? MsalError.ServiceUnavailable
                            : MsalError.Unknown,
                        ErrorDescription = responseStreamString.ToString()
                    };
                }
            }

            return tokenResponse;
        }

        public AuthenticationResultEx GetResultEx()
        {
            AuthenticationResultEx resultEx = null;

            if (this.AccessToken != null)
            {
                DateTimeOffset expiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(this.ExpiresIn);
                var result = new AuthenticationResult(this.TokenType, this.AccessToken, expiresOn);

                result.FamilyId = FamilyId;

                IdToken idToken = IdToken.Parse(this.IdTokenString);
                if (idToken != null)
                {
                    string tenantId = idToken.TenantId;
                    string uniqueId = null;

                    if (!string.IsNullOrWhiteSpace(idToken.ObjectId))
                    {
                        uniqueId = idToken.ObjectId;
                    }
                    else if (!string.IsNullOrWhiteSpace(idToken.Subject))
                    {
                        uniqueId = idToken.Subject;
                    }
                    
                    result.UpdateTenantAndUser(tenantId, this.IdTokenString,
                        new User
                        {
                            UniqueId = uniqueId,
                            DisplayableId = idToken.PreferredUsername,
                            RootId = idToken.HomeObjectId,
                            Name = idToken.Name,
                            IdentityProvider = idToken.Issuer
                        });
                }

                result.ScopeSet = Scope.AsSet();
                resultEx = new AuthenticationResultEx
                {
                    Result = result,
                    RefreshToken = this.RefreshToken
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
