// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Sub-errors send by AAD to indicate than user interaction is required. See https://aka.ms/msal-net-UiRequiredExceptionfor details.
    /// </summary>
    internal static class InvalidGrantClassification
    {
        /// <summary>
        /// Condition can be resolved by user interaction during the interactive authentication flow.
        /// See https://aka.ms/msal-net-UiRequiredExceptionfor details
        /// </summary>
        public const string BasicAction = "basic_action";

        /// <summary>
        /// Condition can be resolved by additional remedial interaction with the system, outside of the interactive authentication flow.
        /// See https://aka.ms/msal-net-UiRequiredExceptionfor details
        /// </summary>
        public const string AdditionalAction = "additional_action";

        /// <summary>
        /// Condition cannot be resolved at this time. Launching interactive authentication flow will show a message explaining the condition.
        /// See https://aka.ms/msal-net-UiRequiredExceptionfor details
        /// </summary>
        public const string MessageOnly = "message_only";

        /// <summary>
        /// User's password has expired.
        /// See https://aka.ms/msal-net-UiRequiredExceptionfor details
        /// </summary>
        public const string UserPasswordExpired = "user_password_expired";

        /// <summary>
        /// User consent is missing, or has been revoked.
        /// See https://aka.ms/msal-net-UiRequiredException for details
        /// </summary>
        public const string ConsentRequired = "consent_required";

        /// <summary>
        /// Internal to MSALs. Indicates that no further silent calls should be made with this refresh token.
        /// </summary>
        internal const string BadToken = "bad_token";

        /// <summary>
        /// Internal to MSALs. Indicates that no further silent calls should be made with this refresh token.
        /// </summary>
        internal const string TokenExpired = "token_expired";

        /// <summary>
        /// Internal to MSALs. Needed in ios/android to complete the end-to-end true MAM flow. This suberror code is re-mapped to a different top level error code (IntuneAppProtectionPoliciesRequired), and not InteractionRequired
        /// </summary>
        internal const string ProtectionPolicyRequired = "protection_policy_required";

        /// <summary>
        /// Internal to MSALs. Used in scenarios where an application is using family refresh token even though it is not part of FOCI (or vice versa). Needed to handle cases where app changes FOCI membership after being shipped. This is handled internally and doesn't need to be exposed to the calling app. Please see FOCI design document for more details.
        /// </summary>
        internal const string ClientMismatch = "client_mismatch";

        /// <summary>
        /// Internal to MSALs. Indicates that device should be re-registered.
        /// </summary>
        internal const string DeviceAuthenticationFailed = "device_authentication_failed";

        internal static bool IsUiInteractionRequired(string subError)
        {
            if (string.IsNullOrEmpty(subError))
            {
                return true;
            }

            return !string.Equals(subError, ClientMismatch, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(subError, ProtectionPolicyRequired, StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetUiExceptionClassification(string subError)
        {
            switch (subError)
            {
                case BasicAction:
                case AdditionalAction:
                case MessageOnly:
                case ConsentRequired:
                case UserPasswordExpired:
                    return subError;

                case BadToken:
                case TokenExpired:
                case ProtectionPolicyRequired:
                case ClientMismatch:
                case DeviceAuthenticationFailed:
                    return string.Empty;

                // Forward compatibility - new sub-errors bubble through
                default:
                    return subError;
            }
        }
    }
}
