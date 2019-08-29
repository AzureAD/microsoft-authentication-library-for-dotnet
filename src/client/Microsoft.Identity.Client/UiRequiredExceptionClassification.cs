// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Details about the cause of an <see cref="MsalUiRequiredException"/>, giving a hint about what the user can expect when 
    /// they go through interactive authentication. See https://aka.ms/msal-net-UiRequiredException for details.
    /// </summary>
    public enum UiRequiredExceptionClassification
    {
        /// <summary>
        /// No further details are provided. It is possible that the user will be able to resolve the issue by launching interactive authentication.
        /// See https://aka.ms/msal-net-UiRequiredException for details
        /// </summary>
        /// <remarks>This is also the classification when no account or valid login hint is passed to AcquireTokenSilent</remarks>
        None,

        /// <summary>
        /// Issue cannot be resolved at this time. Launching interactive authentication flow will show a message explaining the condition.
        /// See https://aka.ms/msal-net-UiRequiredException for details
        /// </summary>
        MessageOnly,

        /// <summary>
        /// Issue can be resolved by user interaction during the interactive authentication flow.
        /// See https://aka.ms/msal-net-UiRequiredException for details
        /// </summary>
        BasicAction,

        /// <summary>
        /// Issue can be resolved by additional remedial interaction with the system, outside of the interactive authentication flow.
        /// Starting an interactive authentication flow will show the user what they need to do, but it is possible that the user is unable to complete the action.
        /// See https://aka.ms/msal-net-UiRequiredException for details
        /// </summary>
        AdditionalAction,

        /// <summary>
        /// User consent is missing, or has been revoked. Issue can be resolved by user consenting during the interactive authentication flow.
        /// See https://aka.ms/msal-net-UiRequiredException for details
        /// </summary>
        ConsentRequired,

        /// <summary>
        /// User's password has expired. Issue can be resolved by user during the interactive authentication flow.
        /// See https://aka.ms/msal-net-UiRequiredException for details.
        /// </summary>
        UserPasswordExpired,

        /// <summary>
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithPrompt(Prompt)"/> was used with Prompt.Never value, 
        /// however this could not be honored by the server. Please use a different prompt behavior, such as <see cref="Prompt.SelectAccount"/>
        /// See https://aka.ms/msal-net-UiRequiredException for details.
        /// </summary>
        PromptNeverFailed,

        /// <summary>
        /// An AcquireTokenSilent call failed. This is ussually part of the pattern 
        /// of calling AcquireTokenSilent for getting a token from the cache, followed by an a different
        /// AcquireToken call for getting a token from AAD. See the error message for details. 
        /// See https://aka.ms/msal-net-UiRequiredException for details.
        /// </summary>
        AcquireTokenSilentFailed
    }
}
