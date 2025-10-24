// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Authentication;
using Microsoft.Authentication.Client;

namespace Microsoft.Identity.Client.Platforms.Features.OneAuthBroker
{
    /// <summary>
    /// OneAuth types - these match the OneAuth API specification
    /// </summary>
    //internal enum AuthenticationScheme
    //{
    //    Bearer = 1,
    //    Pop = 2,
    //    SshCert = 3,
    //    External = 4,
    //    Extension = 5
    //}

    //internal enum RequestOption
    //{
    //    None = 0,
    //    SendX5C = 1
    //}

    //internal enum RequestOptionState
    //{
    //    Disabled = 0,
    //    Enabled = 1
    //}

    //internal enum PreferredAuthMethod
    //{
    //    None = 0,
    //    Account = 1,
    //    Interactive = 2,
    //    Silent = 3
    //}

    /// <summary>
    /// OneAuth AuthenticationParameters class matching the specification provided
    /// </summary>
    //internal sealed class AuthenticationParameters
    //{
    //    /// <summary>
    //    /// Authentication scheme to use for the authentication operation.
    //    /// </summary>
    //    public AuthenticationScheme AuthenticationScheme { get; set; }

    //    /// <summary>
    //    /// Authority URL, can be empty.
    //    /// </summary>
    //    public string Authority { get; set; } = string.Empty;

    //    /// <summary>
    //    /// Target resource that the client is attempting to gain access to.
    //    /// </summary>
    //    public string Target { get; set; } = string.Empty;

    //    /// <summary>
    //    /// Claims to be sent to the authentication server in JSON format.
    //    /// </summary>
    //    public string Claims { get; set; } = string.Empty;

    //    /// <summary>
    //    /// Indicates to the security token provider if your application is capable of complying 
    //    /// and/or participating in/with a particular feature.
    //    /// </summary>
    //    public List<string> Capabilities { get; set; } = new List<string>();

    //    /// <summary>
    //    /// Additional parameters to be sent to the /Authorize endpoint
    //    /// </summary>
    //    public Dictionary<string, string> AdditionalParameters { get; set; } = new Dictionary<string, string>();

    //    /// <summary>
    //    /// Client ID of child nested application for which tokens are being requested
    //    /// </summary>
    //    public string NestedClientId { get; set; } = string.Empty;

    //    /// <summary>
    //    /// Redirect URI of child nested application for which tokens are being requested
    //    /// </summary>
    //    public string NestedRedirectUri { get; set; } = string.Empty;

    //    /// <summary>
    //    /// Request options for the request
    //    /// </summary>
    //    public Dictionary<RequestOption, RequestOptionState> RequestOptions { get; set; } = new Dictionary<RequestOption, RequestOptionState>();

    //    /// <summary>
    //    /// Whether the supplied authority is an ADFS server that supports OIDC.
    //    /// </summary>
    //    public bool IsAdfs { get; set; } = false;

    //    /// <summary>
    //    /// The preferred authentication method for the request
    //    /// </summary>
    //    public PreferredAuthMethod PreferredAuthMethod { get; set; } = PreferredAuthMethod.None;
    //}

    /// <summary>
    /// Maps MSAL.NET parameters to OneAuth parameters using real OneAuth types
    /// </summary>
    internal static class OneAuthParameterMappers
    {
        /// <summary>
        /// Map MSAL parameters to OneAuth configuration (legacy dictionary format for compatibility)
        /// </summary>
        //public static Dictionary<string, object> CreateAuthParameters(AuthenticationRequestParameters authRequestParams)
        //{
        //    var parameters = new Dictionary<string, object>();

        //    if (authRequestParams != null)
        //    {
        //        // Basic parameters that we know MSAL uses
        //        if (!string.IsNullOrEmpty(authRequestParams.AppConfig.ClientId))
        //            parameters["clientId"] = authRequestParams.AppConfig.ClientId;

        //        if (authRequestParams.Scope?.Any() == true)
        //            parameters["scopes"] = authRequestParams.Scope;

        //        if (authRequestParams.RedirectUri != null)
        //            parameters["redirectUri"] = authRequestParams.RedirectUri.ToString();

        //        if (authRequestParams.Authority != null)
        //            parameters["authority"] = authRequestParams.Authority.ToString();

        //        if (!string.IsNullOrEmpty(authRequestParams.LoginHint))
        //            parameters["loginHint"] = authRequestParams.LoginHint;

        //        // Add correlation ID for telemetry
        //        parameters["correlationId"] = authRequestParams.CorrelationId.ToString();
        //    }

        //    return parameters;
        //}

        /// <summary>
        /// Create UX context parameters
        /// </summary>
        //public static Dictionary<string, object> CreateUxContext(AuthenticationRequestParameters authRequestParams)
        //{
        //    var uxContext = new Dictionary<string, object>();

        //    // Basic UX parameters
        //    uxContext["correlationId"] = authRequestParams?.CorrelationId.ToString() ?? Guid.NewGuid().ToString();
        //    uxContext["parentWindowHandle"] = IntPtr.Zero; // Default for now

        //    return uxContext;
        //}

        /// <summary>
        /// Create telemetry parameters
        /// </summary>
        public static Dictionary<string, object> CreateTelemetryParameters(AuthenticationRequestParameters authRequestParams)
        {
            var telemetryParams = new Dictionary<string, object>();

            if (authRequestParams != null)
            {
                telemetryParams["correlationId"] = authRequestParams.CorrelationId.ToString();
                telemetryParams["clientId"] = authRequestParams.AppConfig.ClientId ?? "unknown";
                telemetryParams["apiId"] = authRequestParams.ApiId.ToString();
            }

            return telemetryParams;
        }

        /// <summary>
        /// Create sign-in behavior parameters
        /// </summary>
        //public static Dictionary<string, object> CreateSignInBehaviorParameters(AcquireTokenInteractiveParameters interactiveParams)
        //{
        //    var behaviorParams = new Dictionary<string, object>();

        //    if (interactiveParams != null)
        //    {
        //        // Map prompt behavior
        //        if (interactiveParams.Prompt != null)
        //        {
        //            behaviorParams["prompt"] = interactiveParams.Prompt.ToString();
        //        }

        //        // Add additional interactive parameters that OneAuth might need
        //        if (interactiveParams.Account != null)
        //        {
        //            behaviorParams["accountHint"] = interactiveParams.Account.Username;
        //        }

        //        // Add login hint if available
        //        if (!string.IsNullOrEmpty(interactiveParams.LoginHint))
        //        {
        //            behaviorParams["loginHint"] = interactiveParams.LoginHint;
        //        }

        //        // Add extra scopes to consent if available
        //        if (interactiveParams.ExtraScopesToConsent?.Any() == true)
        //        {
        //            behaviorParams["extraScopesToConsent"] = interactiveParams.ExtraScopesToConsent;
        //        }

        //        // Add web view preferences
        //        behaviorParams["useEmbeddedWebView"] = interactiveParams.UseEmbeddedWebView.ToString();

        //        // Add code verifier if available (for PKCE)
        //        if (!string.IsNullOrEmpty(interactiveParams.CodeVerifier))
        //        {
        //            behaviorParams["codeVerifier"] = interactiveParams.CodeVerifier;
        //        }

        //        // Add custom web UI indicator
        //        if (interactiveParams.CustomWebUi != null)
        //        {
        //            behaviorParams["hasCustomWebUi"] = true;
        //        }
        //    }

        //    return behaviorParams;
        //}

        /// <summary>
        /// Main conversion method that creates OneAuth AuthParameters directly
        /// This maps MSAL parameters to the actual OneAuth package AuthParameters type
        /// </summary>
        //public static Microsoft.OneAuthInterop.AuthParameters ToOneAuthAuthParameters(
        //    AuthenticationRequestParameters authRequestParams,
        //    AcquireTokenInteractiveParameters interactiveParams = null)
        //{
        //    if (authRequestParams == null)
        //    {
        //        throw new ArgumentNullException(nameof(authRequestParams));
        //    }

        //    // Create our internal AuthenticationParameters first
        //    var internalAuthParams = ToOneAuthInternalAuthParameters(authRequestParams, interactiveParams);

        //    // Convert to actual OneAuth AuthParameters
        //    return ToOneAuthAuthParams(internalAuthParams);
        //}

        public static Microsoft.Authentication.Client.AuthenticationParameters CreateDirectOneAuthParameters(
            AuthenticationRequestParameters authRequestParams,
            ILoggerAdapter logger)
        {
            if (authRequestParams == null)
            {
                throw new ArgumentNullException(nameof(authRequestParams));
            }

            var oneAuthParams = new Microsoft.Authentication.Client.AuthenticationParameters();

            // 2. Authority - Extract canonical authority URL (critical for guest tenant scenarios)
            oneAuthParams.Authority = authRequestParams.Authority?.AuthorityInfo?.CanonicalAuthority?.ToString() ?? string.Empty;

            // 3. Target - Combine MSAL scopes into space-separated string per OAuth2 spec
            oneAuthParams.Target = authRequestParams.Scope?.Any() == true ?
                string.Join(" ", authRequestParams.Scope) : string.Empty;

            // 4. AccessTokenToRenew - Not used in MSAL scenarios, leave empty
            oneAuthParams.AccessTokenToRenew = string.Empty;

            // 5. Claims - Use merged claims and client capabilities from MSAL in JSON format
            oneAuthParams.Claims = authRequestParams.ClaimsAndClientCapabilities ?? string.Empty;

            // 6. Capabilities - Extract MSAL client capabilities (long lived tokens, True MAM, etc.)
            oneAuthParams.Capabilities = authRequestParams.AppConfig?.ClientCapabilities?.ToList() ?? new List<string>();

            // 7. AdditionalParameters - Build OAuth2 /Authorize endpoint parameters
            oneAuthParams.AdditionalParameters = BuildOneAuthAdditionalParameters(authRequestParams, logger);

            // 8. PopParameters - Set to null (would need specific PoP token configuration)
            //oneAuthParams. = null;

            // 9. NestedClientId - Empty for standard scenarios (used for child nested apps)
            oneAuthParams.NestedClientId = string.Empty;

            // 10. NestedRedirectUri - Empty for standard scenarios (used for child nested apps)
            oneAuthParams.NestedRedirectUri = string.Empty;

            // 11. RequestOptions - Map MSAL request options to OneAuth format
            //oneAuthParams.RequestOptions = BuildOneAuthRequestOptions(authRequestParams);

            // 12. IsAdfs - Detect if authority is ADFS server with OIDC support
            oneAuthParams.IsAdfs = authRequestParams.Authority?.AuthorityInfo?.AuthorityType == AuthorityType.Adfs;

            oneAuthParams.AuthenticationScheme = authRequestParams.AuthenticationScheme?.AccessTokenType switch
            {
                "basic" => Microsoft.Authentication.Client.AuthenticationScheme.Basic,
                "negotiate" => Microsoft.Authentication.Client.AuthenticationScheme.Negotiate,
                "ntlm" => Microsoft.Authentication.Client.AuthenticationScheme.Ntlm,
                "liveid" => Microsoft.Authentication.Client.AuthenticationScheme.LiveId,
                _ => Microsoft.Authentication.Client.AuthenticationScheme.Bearer, // Default to Bearer
            };
            // 13. PreferredAuthMethod - Determine optimal authentication method
            //oneAuthParams.PreferredAuthMethod = DetermineOneAuthPreferredAuthMethod(authRequestParams, interactiveParams);

            return oneAuthParams;
        }

        /// <summary>
        /// Maps MSAL IAuthenticationOperation to OneAuth AuthenticationScheme enum
        /// Uses the actual Microsoft.OneAuthInterop.AuthScheme enum values
        /// </summary>
        //private static Microsoft.OneAuthInterop.AuthScheme MapMsalToOneAuthScheme(Microsoft.Identity.Client.AuthScheme.IAuthenticationOperation authOperation)
        //{
        //    if (authOperation == null)
        //        return Microsoft.OneAuthInterop.AuthScheme.Bearer; // Default to Bearer (OAuth2)

        //    // Map MSAL access token type to OneAuth AuthScheme
        //    return authOperation.AccessTokenType?.ToLowerInvariant() switch
        //    {
        //        "bearer" => Microsoft.OneAuthInterop.AuthScheme.Bearer,  // OAuth2
        //        "pop" => Microsoft.OneAuthInterop.AuthScheme.PoP,       // PoP with OAuth2
        //        "basic" => Microsoft.OneAuthInterop.AuthScheme.Basic,   // HTTP Basic
        //        "negotiate" => Microsoft.OneAuthInterop.AuthScheme.Negotiate, // SPNEGO
        //        "ntlm" => Microsoft.OneAuthInterop.AuthScheme.Ntlm,     // Windows Challenge/Response
        //        "liveid" => Microsoft.OneAuthInterop.AuthScheme.LiveId, // LiveId RPS tokens
        //        _ => Microsoft.OneAuthInterop.AuthScheme.Bearer         // Default to Bearer
        //    };
        //}

        /// <summary>
        /// Builds additional parameters dictionary for OneAuth /Authorize endpoint
        /// Maps core OAuth2/OIDC parameters and MSAL-specific parameters
        /// </summary>
        private static Dictionary<string, string> BuildOneAuthAdditionalParameters(
            AuthenticationRequestParameters authRequestParams,
             ILoggerAdapter logger)
        {
            var parameters = new Dictionary<string, string>();

            // Core OAuth2/OIDC parameters
            //if (!string.IsNullOrEmpty(authRequestParams.AppConfig?.ClientId))
            //    parameters["client_id"] = authRequestParams.AppConfig.ClientId;

            //if (authRequestParams.RedirectUri != null)
            //    parameters["redirect_uri"] = authRequestParams.RedirectUri.ToString();

            // Tracking and correlation for debugging
            parameters["correlation_id"] = authRequestParams.CorrelationId.ToString();

            if (authRequestParams.AppConfig.MultiCloudSupportEnabled)
            {
                parameters["instance_aware"] = "true";
            }

            //pass client sku and ver
            Dictionary<string, string> msalIdParams = MsalIdHelper.GetMsalIdParameters(logger);
            parameters["msal_client_sku"] = msalIdParams[MsalIdParameter.Product];
            parameters["msal_client_ver"] = msalIdParams[MsalIdParameter.Version];

            if(authRequestParams.AuthenticationScheme.AccessTokenType == "ssh-cert")
            {
                parameters["key_id"] = authRequestParams.AuthenticationScheme.KeyId;
                foreach (KeyValuePair<string, string> kvp in authRequestParams.AuthenticationScheme.GetTokenRequestParams())
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }

            // MSAL extra query parameters (preserve app-specific parameters)
            if (authRequestParams.ExtraQueryParameters != null)
            {
                foreach (KeyValuePair<string, string> kvp in authRequestParams.ExtraQueryParameters)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }

            return parameters;
        }

        /// <summary>
        /// Maps MSAL request options to OneAuth request options
        /// </summary>
        //private static Dictionary<RequestOption, RequestOptionState> MapRequestOptions(
        //    AuthenticationRequestParameters authRequestParams)
        //{
        //    var requestOptions = new Dictionary<RequestOption, RequestOptionState>();

        //    // Add request options based on MSAL configuration
        //    if (authRequestParams.SendX5C)
        //    {
        //        requestOptions[RequestOption.SendX5C] = RequestOptionState.Enabled;
        //    }

        //    return requestOptions;
        //}
    }
}
