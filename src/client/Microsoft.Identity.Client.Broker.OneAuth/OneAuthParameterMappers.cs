// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.Platforms.Features.OneAuthBroker
{
    /// <summary>
    /// Maps MSAL.NET parameters to OneAuth parameters
    /// This is a placeholder implementation for build compatibility
    /// </summary>
    internal static class OneAuthParameterMappers
    {
        /// <summary>
        /// Map MSAL parameters to OneAuth configuration
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

                // For now, we'll omit ExtraQueryParameters as it doesn't exist on the current type
                // This can be added back when we understand the real OneAuth API
            }

            return behaviorParams;
        }
    }
}
