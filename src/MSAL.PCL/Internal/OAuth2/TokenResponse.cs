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
using System.Runtime.Serialization;

namespace Microsoft.Identity.Client.Internal.OAuth2
{
    internal class TokenResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string Code = "code";
        public const string TokenType = "token_type";
        public const string AccessToken = "access_token";
        public const string RefreshToken = "refresh_token";
        public const string Scope = "scope";
        public const string FamilyId = "foci";
        public const string IdToken = "id_token";
        public const string ExpiresIn = "expires_in";
        public const string IdTokenExpiresIn = "id_token_expires_in";
    }

    [DataContract]
    internal class TokenResponse : OAuth2ResponseBase
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
        public string IdToken { get; set; }

        [DataMember(Name = TokenResponseClaim.ExpiresIn, IsRequired = false)]
        public long ExpiresIn { get; set; }

        [DataMember(Name = TokenResponseClaim.IdTokenExpiresIn, IsRequired = false)]
        public long IdTokenExpiresIn { get; set; }

        public DateTimeOffset AccessTokenExpiresOn { get { return DateTime.UtcNow + TimeSpan.FromSeconds(this.ExpiresIn); } }

        public DateTimeOffset IdTokenExpiresOn { get { return DateTime.UtcNow + TimeSpan.FromSeconds(this.IdTokenExpiresIn); } }


        public AuthenticationResultEx GetResultEx()
        {
            AuthenticationResultEx resultEx = null;

            if (!string.IsNullOrEmpty(this.AccessToken) || !string.IsNullOrEmpty(this.IdToken))
            {
                DateTimeOffset accessTokenExpiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(this.ExpiresIn);
                DateTimeOffset idTokenExpiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(this.IdTokenExpiresIn);

                AuthenticationResult result = null;
                if (!string.IsNullOrEmpty(this.AccessToken))
                {
                    result = new AuthenticationResult(this.TokenType, this.AccessToken, accessTokenExpiresOn);
                }
                else
                {
                    result = new AuthenticationResult(this.TokenType, this.IdToken, idTokenExpiresOn);
                }


                result.FamilyId = FamilyId;
                IdToken idToken = Internal.IdToken.Parse(this.IdToken);
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

                    if (string.IsNullOrWhiteSpace(idToken.HomeObjectId))
                    {
                        idToken.HomeObjectId = uniqueId;
                    }

                    result.UpdateTenantAndUser(tenantId, this.IdToken,
                        new User
                        {
                            UniqueId = uniqueId,
                            DisplayableId = idToken.PreferredUsername,
                            HomeObjectId = idToken.HomeObjectId,
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
    }
}