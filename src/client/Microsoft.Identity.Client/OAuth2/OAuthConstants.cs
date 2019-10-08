// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.OAuth2
{
    internal static class OAuth2Parameter
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
        public const string DeviceCode = "device_code";
        public const string Scope = "scope";
        public const string Assertion = "assertion";
        public const string RequestedTokenUse = "requested_token_use";
        public const string Username = "username";
        public const string Password = "password";
        public const string LoginHint = "login_hint"; // login_hint is not standard oauth2 parameter
        public const string CorrelationId = OAuth2Header.CorrelationId;
        public const string State = "state";

        public const string CodeChallengeMethod = "code_challenge_method";
        public const string CodeChallenge = "code_challenge";
        public const string CodeVerifier = "code_verifier";
        // correlation id is not standard oauth2 parameter
        public const string LoginReq = "login_req";
        public const string DomainReq = "domain_req";

        public const string Prompt = "prompt"; // prompt is not standard oauth2 parameter
        public const string ClientInfo = "client_info"; // restrict_to_hint is not standard oauth2 parameter

        public const string Claims = "claims"; // claims is not a standard oauth2 paramter

        public const string TokenType = "token_type"; // not a standard OAuth2 param
        public const string RequestConfirmation = "req_cnf"; // not a standard OAuth2 param
    }

    internal static class OAuth2GrantType
    {
        public const string AuthorizationCode = "authorization_code";
        public const string RefreshToken = "refresh_token";
        public const string ClientCredentials = "client_credentials";
        public const string Saml11Bearer = "urn:ietf:params:oauth:grant-type:saml1_1-bearer";
        public const string Saml20Bearer = "urn:ietf:params:oauth:grant-type:saml2-bearer";
        public const string JwtBearer = "urn:ietf:params:oauth:grant-type:jwt-bearer";
        public const string Password = "password";
        public const string DeviceCode = "device_code";
    }

    internal static class OAuth2ResponseType
    {
        public const string Code = "code";
    }

    internal static class OAuth2AssertionType
    {
        public const string JwtBearer = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
    }

    internal static class OAuth2RequestedTokenUse
    {
        public const string OnBehalfOf = "on_behalf_of";
    }

    internal static class OAuth2Header
    {
        public const string CorrelationId = "client-request-id";
        public const string RequestCorrelationIdInResponse = "return-client-request-id";
        public const string AppName = "x-app-name";
        public const string AppVer = "x-app-ver";
    }

    /// <summary>
    /// OAuth2 errors that are only used internally. All error codes used when propagating exceptions should
    /// be made public.
    /// </summary>
    internal static class OAuth2Error
    {
        public const string LoginRequired = "login_required";
        public const string AuthorizationPending = "authorization_pending";
    }

    internal static class OAuth2Value
    {
        public static readonly SortedSet<string> ReservedScopes = 
            new SortedSet<string> { ScopeOpenId, ScopeProfile, ScopeOfflineAccess };
        public const string CodeChallengeMethodValue = "S256";
        public const string ScopeOpenId = "openid";
        public const string ScopeOfflineAccess = "offline_access";
        public const string ScopeProfile = "profile";
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
