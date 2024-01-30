// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace Microsoft.Identity.Client
{
    internal static class LogMessages
    {
        public const string BeginningAcquireByRefreshToken = "Begin acquire token by refresh token...";
        public const string NoScopesProvidedForRefreshTokenRequest = "No scopes provided for acquire token by refresh token request. " +
            "Using default scope instead.";      

        public const string CustomWebUiAcquiringAuthorizationCode = "Using CustomWebUi to acquire the authorization code";
        public const string CustomWebUiRedirectUriMatched = "Redirect Uri was matched.  Returning success from CustomWebUiHandler.";
        public const string CustomWebUiOperationCancelled = "CustomWebUi AcquireAuthorizationCode was canceled";

        public const string CustomWebUiCallingAcquireAuthorizationCodeNoPii = "Calling CustomWebUi.AcquireAuthorizationCode";

        public const string ClientAssertionDoesNotExistOrNearExpiry = "Client Assertion does not exist or near expiry. ";
        public const string ReusingTheUnexpiredClientAssertion = "Reusing the unexpired Client Assertion...";

        public const string ResolvingAuthorityEndpointsTrue = "Resolving authority endpoints... Already resolved? - TRUE";
        public const string ResolvingAuthorityEndpointsFalse = "Resolving authority endpoints... Already resolved? - FALSE";

        public const string CheckMsalTokenResponseReturnedFromBroker = "Checking MsalTokenResponse returned from broker. ";
        public const string UnknownErrorReturnedInBrokerResponse = "Unknown error returned in broker response. ";
        public const string BrokerInvocationRequired = "Based on auth code received from STS, broker invocation is required. ";
        public const string AddBrokerInstallUrlToPayload = "Broker is required for authentication and broker is not installed on the device. " +
            "Adding BrokerInstallUrl to broker payload. ";
        public const string BrokerInvocationNotRequired = "Based on auth code received from STS, broker invocation is not required. ";
        public const string CanInvokeBrokerAcquireTokenWithBroker = "Can invoke broker. Will attempt to acquire token with broker. ";
        public const string AuthenticationWithBrokerDidNotSucceed = "Broker authentication did not succeed, or the broker install failed. " +
            "See https://aka.ms/msal-net-brokers for more information. ";
        public const string UserCancelledAuthentication = "Authorization result status returned user cancelled authentication. ";
        public const string AuthorizationResultWasNotSuccessful = "Authorization result was not successful. See error message for more details. ";

        public const string WsTrustRequestFailed = "Ws-Trust request failed. See error message for more details.";

        public static string ErrorReturnedInBrokerResponse(string error)
        {
            return $"Error {error} returned in broker response. ";
        }

        public static string UsingXScopesForRefreshTokenRequest(int numScopes)
        {
            return string.Format(CultureInfo.InvariantCulture, "Using {0} scopes for acquire token by refresh token request", numScopes);
        }
        public static string CustomWebUiCallingAcquireAuthorizationCodePii(Uri authorizationUri, Uri redirectUri)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "calling CustomWebUi.AcquireAuthorizationCode authUri({0}) redirectUri({1})",
                authorizationUri,
                redirectUri);
        }
    }
}
