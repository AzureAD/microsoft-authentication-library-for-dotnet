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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class OAuthParameter
    {
        public const string ResponseType = "response_type";
        public const string GrantType = "grant_type";
        public const string ClientId = "client_id";
        public const string ClientSecret = "client_secret";
        public const string ClientAssertion = "client_assertion";
        public const string ClientAssertionType = "client_assertion_type";
        public const string RefreshToken = "refresh_token";
        public const string RedirectUri = "redirect_uri";
        public const string Resource = "resource";
        public const string Code = "code";
        public const string Scope = "scope";
        public const string Assertion = "assertion";
        public const string RequestedTokenUse = "requested_token_use";
        public const string Username = "username";
        public const string Password = "password";

        public const string FormsAuth = "amr_values";
        public const string LoginHint = "login_hint"; // login_hint is not standard oauth2 parameter
        public const string CorrelationId = OAuthHeader.CorrelationId; // correlation id is not standard oauth2 parameter
        public const string Prompt = "prompt"; // prompt is not standard oauth2 parameter
    }

    internal class OAuthGrantType
    {
        public const string AuthorizationCode = "authorization_code";
        public const string RefreshToken = "refresh_token";
        public const string ClientCredentials = "client_credentials";
        public const string Saml11Bearer = "urn:ietf:params:oauth:grant-type:saml1_1-bearer";
        public const string Saml20Bearer = "urn:ietf:params:oauth:grant-type:saml2-bearer";
        public const string JwtBearer = "urn:ietf:params:oauth:grant-type:jwt-bearer";
        public const string Password = "password";
    }

    internal class OAuthResponseType
    {
        public const string Code = "code";
    }

    internal class OAuthReservedClaim
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

    internal class IdTokenClaim
    {
        public const string ObjectId = "oid";
        public const string Subject = "sub";
        public const string TenantId = "tid";
        public const string UPN = "upn";
        public const string Email = "email";
        public const string GivenName = "given_name";
        public const string FamilyName = "family_name";
        public const string IdentityProvider = "idp";
        public const string Issuer = "iss";
        public const string PasswordExpiration = "pwd_exp";
        public const string PasswordChangeUrl = "pwd_url";
    }

    internal class OAuthAssertionType
    {
        public const string JwtBearer = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
    }

    internal class OAuthRequestedTokenUse
    {
        public const string OnBehalfOf = "on_behalf_of";
    }

    internal class OAuthHeader
    {
        public const string CorrelationId = "client-request-id";
        public const string RequestCorrelationIdInResponse = "return-client-request-id";
    }

    internal class OAuthError
    {
        public const string LoginRequired = "login_required";
    }

    internal class OAuthValue
    {
        public const string FormsAuth = "pwd";
        public const string ScopeOpenId = "openid";
    }

    internal class PromptValue
    {
        public const string Login = "login";
        public const string RefreshSession = "refresh_session";

        // The behavior of this value is identical to prompt=none for managed users; However, for federated users, AAD
        // redirects to ADFS as it cannot determine in advance whether ADFS can login user silently (e.g. via WIA) or not.
        public const string AttemptNone = "attempt_none";        
    }
}