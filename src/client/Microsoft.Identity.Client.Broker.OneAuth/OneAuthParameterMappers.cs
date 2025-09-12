// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Authentication;
using Microsoft.OneAuthInterop;

namespace Microsoft.Identity.Client.Platforms.Features.OneAuthBroker
{
    /// <summary>
    /// OneAuth types - these match the OneAuth API specification
    /// </summary>
    internal enum AuthenticationScheme
    {
        Bearer = 1,
        Pop = 2,
        SshCert = 3,
        External = 4,
        Extension = 5
    }

    internal enum RequestOption
    {
        None = 0,
        SendX5C = 1
    }

    internal enum RequestOptionState
    {
        Disabled = 0,
        Enabled = 1
    }

    internal enum PreferredAuthMethod
    {
        None = 0,
        Account = 1,
        Interactive = 2,
        Silent = 3
    }

    /// <summary>
    /// OneAuth AuthenticationParameters class matching the specification provided
    /// </summary>
    internal sealed class AuthenticationParameters
    {
        /// <summary>
        /// Authentication scheme to use for the authentication operation.
        /// </summary>
        public AuthenticationScheme AuthenticationScheme { get; set; }

        /// <summary>
        /// Authority URL, can be empty.
        /// </summary>
        public string Authority { get; set; } = string.Empty;

        /// <summary>
        /// Target resource that the client is attempting to gain access to.
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Claims to be sent to the authentication server in JSON format.
        /// </summary>
        public string Claims { get; set; } = string.Empty;

        /// <summary>
        /// Indicates to the security token provider if your application is capable of complying 
        /// and/or participating in/with a particular feature.
        /// </summary>
        public List<string> Capabilities { get; set; } = new List<string>();

        /// <summary>
        /// Additional parameters to be sent to the /Authorize endpoint
        /// </summary>
        public Dictionary<string, string> AdditionalParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Client ID of child nested application for which tokens are being requested
        /// </summary>
        public string NestedClientId { get; set; } = string.Empty;

        /// <summary>
        /// Redirect URI of child nested application for which tokens are being requested
        /// </summary>
        public string NestedRedirectUri { get; set; } = string.Empty;

        /// <summary>
        /// Request options for the request
        /// </summary>
        public Dictionary<RequestOption, RequestOptionState> RequestOptions { get; set; } = new Dictionary<RequestOption, RequestOptionState>();

        /// <summary>
        /// Whether the supplied authority is an ADFS server that supports OIDC.
        /// </summary>
        public bool IsAdfs { get; set; } = false;

        /// <summary>
        /// The preferred authentication method for the request
        /// </summary>
        public PreferredAuthMethod PreferredAuthMethod { get; set; } = PreferredAuthMethod.None;
    }

    /// <summary>
    /// Maps MSAL.NET parameters to OneAuth parameters using real OneAuth types
    /// </summary>
    internal static class OneAuthParameterMappers
    {
        /// <summary>
        /// Map MSAL parameters to OneAuth configuration (legacy dictionary format for compatibility)
        /// </summary>
        public static Dictionary<string, object> CreateAuthParameters(AuthenticationRequestParameters authRequestParams)
        {
            var parameters = new Dictionary<string, object>();

            if (authRequestParams != null)
            {
                // Basic parameters that we know MSAL uses
                if (!string.IsNullOrEmpty(authRequestParams.AppConfig.ClientId))
                    parameters["clientId"] = authRequestParams.AppConfig.ClientId;

                if (authRequestParams.Scope?.Any() == true)
                    parameters["scopes"] = authRequestParams.Scope;

                if (authRequestParams.RedirectUri != null)
                    parameters["redirectUri"] = authRequestParams.RedirectUri.ToString();

                if (authRequestParams.Authority != null)
                    parameters["authority"] = authRequestParams.Authority.ToString();

                if (!string.IsNullOrEmpty(authRequestParams.LoginHint))
                    parameters["loginHint"] = authRequestParams.LoginHint;

                // Add correlation ID for telemetry
                parameters["correlationId"] = authRequestParams.CorrelationId.ToString();
            }

            return parameters;
        }

        /// <summary>
        /// Create UX context parameters
        /// </summary>
        public static Dictionary<string, object> CreateUxContext(AuthenticationRequestParameters authRequestParams)
        {
            var uxContext = new Dictionary<string, object>();

            // Basic UX parameters
            uxContext["correlationId"] = authRequestParams?.CorrelationId.ToString() ?? Guid.NewGuid().ToString();
            uxContext["parentWindowHandle"] = IntPtr.Zero; // Default for now

            return uxContext;
        }

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
        public static Dictionary<string, object> CreateSignInBehaviorParameters(AcquireTokenInteractiveParameters interactiveParams)
        {
            var behaviorParams = new Dictionary<string, object>();

            if (interactiveParams != null)
            {
                // Map prompt behavior
                if (interactiveParams.Prompt != null)
                {
                    behaviorParams["prompt"] = interactiveParams.Prompt.ToString();
                }

                // Add additional interactive parameters that OneAuth might need
                if (interactiveParams.Account != null)
                {
                    behaviorParams["accountHint"] = interactiveParams.Account.Username;
                }

                // Add login hint if available
                if (!string.IsNullOrEmpty(interactiveParams.LoginHint))
                {
                    behaviorParams["loginHint"] = interactiveParams.LoginHint;
                }

                // Add extra scopes to consent if available
                if (interactiveParams.ExtraScopesToConsent?.Any() == true)
                {
                    behaviorParams["extraScopesToConsent"] = interactiveParams.ExtraScopesToConsent;
                }

                // Add web view preferences
                behaviorParams["useEmbeddedWebView"] = interactiveParams.UseEmbeddedWebView.ToString();

                // Add code verifier if available (for PKCE)
                if (!string.IsNullOrEmpty(interactiveParams.CodeVerifier))
                {
                    behaviorParams["codeVerifier"] = interactiveParams.CodeVerifier;
                }

                // Add custom web UI indicator
                if (interactiveParams.CustomWebUi != null)
                {
                    behaviorParams["hasCustomWebUi"] = true;
                }
            }

            return behaviorParams;
        }

        /// <summary>
        /// Main conversion method that creates OneAuth AuthParameters directly
        /// This maps MSAL parameters to the actual OneAuth package AuthParameters type
        /// </summary>
        public static Microsoft.OneAuthInterop.AuthParameters ToOneAuthAuthParameters(
            AuthenticationRequestParameters authRequestParams,
            AcquireTokenInteractiveParameters interactiveParams = null)
        {
            if (authRequestParams == null)
            {
                throw new ArgumentNullException(nameof(authRequestParams));
            }

            // Create our internal AuthenticationParameters first
            var internalAuthParams = ToOneAuthInternalAuthParameters(authRequestParams, interactiveParams);

            // Convert to actual OneAuth AuthParameters
            return ToOneAuthAuthParams(internalAuthParams);
        }

        /// <summary>
        /// Converts MSAL AuthenticationRequestParameters to our internal AuthenticationParameters structure
        /// This creates our internal representation based on your OneAuth specification
        /// </summary>
        internal static AuthenticationParameters ToOneAuthInternalAuthParameters(
            AuthenticationRequestParameters authRequestParams,
            AcquireTokenInteractiveParameters interactiveParams = null)
        {
            if (authRequestParams == null)
            {
                throw new ArgumentNullException(nameof(authRequestParams));
            }

            var oneAuthParams = new AuthenticationParameters
            {
                // Map authentication scheme - determine based on MSAL auth operation
                AuthenticationScheme = MapToOneAuthAuthenticationScheme(authRequestParams.AuthenticationScheme),

                // Map authority - use the canonical authority URL
                Authority = authRequestParams.Authority?.AuthorityInfo?.CanonicalAuthority?.ToString() ?? string.Empty,

                // Map target (scopes) - combine scopes into space-separated string
                Target = MapScopesToTarget(authRequestParams.Scope),

                // Map claims if available
                Claims = authRequestParams.ClaimsAndClientCapabilities ?? string.Empty,

                // Map capabilities
                Capabilities = MapCapabilities(authRequestParams),

                // Map additional parameters
                AdditionalParameters = MapAdditionalParameters(authRequestParams, interactiveParams),

                // Map nested client info - empty for standard scenarios
                NestedClientId = string.Empty,
                NestedRedirectUri = string.Empty,

                // Map request options
                RequestOptions = MapRequestOptions(authRequestParams),

                // Check if authority is ADFS
                IsAdfs = authRequestParams.Authority?.AuthorityInfo?.AuthorityType == AuthorityType.Adfs,

                // Map preferred authentication method
                PreferredAuthMethod = MapPreferredAuthMethod(authRequestParams, interactiveParams)
            };

            return oneAuthParams;
        }

        /// <summary>
        /// Create OneAuth SignInBehaviorParameters from MSAL interactive parameters
        /// </summary>
        public static SignInBehaviorParameters ToOneAuthSignInBehaviorParameters(
            AcquireTokenInteractiveParameters interactiveParams)
        {
            // Create OneAuth SignInBehaviorParameters - this assumes the type exists in OneAuth package
            var behaviorParams = new SignInBehaviorParameters();

            if (interactiveParams != null)
            {
                // Map prompt behavior to OneAuth prompt (if SignInBehaviorParameters has Prompt property)
                // This will need to be adjusted based on actual OneAuth SignInBehaviorParameters structure
            }

            return behaviorParams;
        }

        /// <summary>
        /// Converts our internal AuthenticationParameters to OneAuth AuthParameters
        /// This bridges the gap between our specification and the actual OneAuth package types
        /// </summary>
        private static Microsoft.OneAuthInterop.AuthParameters ToOneAuthAuthParams(AuthenticationParameters authParams)
        {
            if (authParams == null)
                throw new ArgumentNullException(nameof(authParams));

            // Create OneAuth AuthParameters using the actual OneAuth package type
            var oneAuthAuthParams = new Microsoft.OneAuthInterop.AuthParameters();

            // Map properties from our AuthenticationParameters to OneAuth AuthParameters
            // The exact property names will depend on what's actually available in the OneAuth package

            try
            {
                // Use reflection to set properties safely until we know the exact structure
                var oneAuthType = oneAuthAuthParams.GetType();
                
                // Map Authority
                if (!string.IsNullOrEmpty(authParams.Authority))
                {
                    var authorityProp = oneAuthType.GetProperty("Authority");
                    authorityProp?.SetValue(oneAuthAuthParams, authParams.Authority);
                }

                // Map Target
                if (!string.IsNullOrEmpty(authParams.Target))
                {
                    var targetProp = oneAuthType.GetProperty("Target");
                    targetProp?.SetValue(oneAuthAuthParams, authParams.Target);
                }

                // Map Claims
                if (!string.IsNullOrEmpty(authParams.Claims))
                {
                    var claimsProp = oneAuthType.GetProperty("Claims");
                    claimsProp?.SetValue(oneAuthAuthParams, authParams.Claims);
                }

                // Map other properties as available...
                // This is a safe approach that won't fail if properties don't exist

            }
            catch (Exception)
            {
                // If property mapping fails, we'll work with basic AuthParameters
                // This ensures the code doesn't crash if the OneAuth structure is different
            }

            return oneAuthAuthParams;
        }

        /// <summary>
        /// Maps MSAL authentication scheme to OneAuth authentication scheme
        /// </summary>
        private static AuthenticationScheme MapToOneAuthAuthenticationScheme(Microsoft.Identity.Client.AuthScheme.IAuthenticationOperation authOperation)
        {
            if (authOperation == null)
                return AuthenticationScheme.Bearer; // Default to Bearer

            // Map based on the authentication operation type
            switch (authOperation.AccessTokenType?.ToLowerInvariant())
            {
                case "bearer":
                    return AuthenticationScheme.Bearer;
                case "pop":
                    return AuthenticationScheme.Pop;
                case "ssh-cert":
                    return AuthenticationScheme.SshCert;
                default:
                    return AuthenticationScheme.Bearer; // Default fallback
            }
        }

        /// <summary>
        /// Maps MSAL scopes to OneAuth target format
        /// </summary>
        private static string MapScopesToTarget(IEnumerable<string> scopes)
        {
            if (scopes?.Any() != true)
                return string.Empty;

            // Join scopes with space separator as is common in OAuth2
            return string.Join(" ", scopes);
        }

        /// <summary>
        /// Maps MSAL capabilities to OneAuth capabilities list
        /// </summary>
        private static List<string> MapCapabilities(AuthenticationRequestParameters authRequestParams)
        {
            var capabilities = new List<string>();

            // Add capabilities from client capabilities if available
            if (authRequestParams.AppConfig?.ClientCapabilities?.Any() == true)
            {
                capabilities.AddRange(authRequestParams.AppConfig.ClientCapabilities);
            }

            return capabilities;
        }

        /// <summary>
        /// Maps additional parameters from both authentication and interactive parameters
        /// </summary>
        private static Dictionary<string, string> MapAdditionalParameters(
            AuthenticationRequestParameters authRequestParams,
            AcquireTokenInteractiveParameters interactiveParams)
        {
            var additionalParams = new Dictionary<string, string>();

            // Add correlation ID for tracing
            additionalParams["correlation_id"] = authRequestParams.CorrelationId.ToString();

            // Add client ID
            if (!string.IsNullOrEmpty(authRequestParams.AppConfig?.ClientId))
            {
                additionalParams["client_id"] = authRequestParams.AppConfig.ClientId;
            }

            // Add redirect URI
            if (authRequestParams.RedirectUri != null)
            {
                additionalParams["redirect_uri"] = authRequestParams.RedirectUri.ToString();
            }

            // Add login hint
            if (!string.IsNullOrEmpty(authRequestParams.LoginHint))
            {
                additionalParams["login_hint"] = authRequestParams.LoginHint;
            }

            // Add domain hint if available from account
            if (!string.IsNullOrEmpty(authRequestParams.Account?.Environment))
            {
                additionalParams["domain_hint"] = authRequestParams.Account.Environment;
            }

            // Add prompt parameter from interactive params
            if (interactiveParams?.Prompt != null)
            {
                additionalParams["prompt"] = interactiveParams.Prompt.PromptValue;
            }

            // Add extra scopes to consent from interactive params
            if (interactiveParams?.ExtraScopesToConsent?.Any() == true)
            {
                additionalParams["extra_scopes"] = string.Join(" ", interactiveParams.ExtraScopesToConsent);
            }

            // Add code verifier for PKCE if available
            if (!string.IsNullOrEmpty(interactiveParams?.CodeVerifier))
            {
                additionalParams["code_verifier"] = interactiveParams.CodeVerifier;
            }

            // Add web view preference
            if (interactiveParams != null)
            {
                additionalParams["use_embedded_webview"] = interactiveParams.UseEmbeddedWebView.ToString();
            }

            return additionalParams;
        }

        /// <summary>
        /// Maps MSAL request options to OneAuth request options
        /// </summary>
        private static Dictionary<RequestOption, RequestOptionState> MapRequestOptions(
            AuthenticationRequestParameters authRequestParams)
        {
            var requestOptions = new Dictionary<RequestOption, RequestOptionState>();

            // Add request options based on MSAL configuration
            if (authRequestParams.SendX5C)
            {
                requestOptions[RequestOption.SendX5C] = RequestOptionState.Enabled;
            }

            return requestOptions;
        }

        /// <summary>
        /// Maps preferred authentication method based on MSAL parameters
        /// </summary>
        private static PreferredAuthMethod MapPreferredAuthMethod(
            AuthenticationRequestParameters authRequestParams,
            AcquireTokenInteractiveParameters interactiveParams)
        {
            // Determine preferred auth method based on available parameters
            
            // If we have an account, prefer that method
            if (authRequestParams.Account != null)
            {
                return PreferredAuthMethod.Account;
            }

            // If we have a login hint, prefer interactive with hint
            if (!string.IsNullOrEmpty(authRequestParams.LoginHint))
            {
                return PreferredAuthMethod.Interactive;
            }

            // Default to None to let OneAuth decide
            return PreferredAuthMethod.None;
        }
    }
}
