// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Details about the cause of an <see cref="MsalUiRequiredException"/>, giving a hint about what the user can expect when 
    /// they go through interactive authentication. See <see href="https://aka.ms/msal-net-UiRequiredException">Understanding MsalUiRequiredException</see> for details.
    /// </summary>
    public enum UiRequiredExceptionClassification
    {
        /// <summary>
        /// No details are provided. It is possible that the user will be able to resolve the issue by launching interactive authentication.
        /// This is also the classification when no account or valid login hint is passed to <see cref="AcquireTokenSilentParameterBuilder"/>.
        /// See <see href="https://aka.ms/msal-net-UiRequiredException">Understanding MsalUiRequiredException</see> for details.
        /// </summary>
        None,

        /// <summary>
        /// Issue cannot be resolved. Launching interactive authentication flow will show a message explaining the condition.
        /// See <see href="https://aka.ms/msal-net-UiRequiredException">Understanding MsalUiRequiredException</see> for details.
        /// </summary>
        MessageOnly,

        /// <summary>
        /// Issue can be resolved by user interaction during the interactive authentication flow.
        /// See <see href="https://aka.ms/msal-net-UiRequiredException">Understanding MsalUiRequiredException</see> for details.
        /// </summary>
        BasicAction,

        /// <summary>
        /// Issue can be resolved by additional remedial interaction within the system, outside of the interactive authentication flow.
        /// Starting an interactive authentication flow will show the user what they need to do but it is possible that the user will be unable to complete the action.
        /// See <see href="https://aka.ms/msal-net-UiRequiredException">Understanding MsalUiRequiredException</see> for details.
        /// </summary>
        AdditionalAction,

        /// <summary>
        /// User consent is missing or has been revoked. Issue can be resolved by user consenting during the interactive authentication flow.
        /// See <see href="https://aka.ms/msal-net-UiRequiredException">Understanding MsalUiRequiredException</see> for details.
        /// </summary>
        ConsentRequired,

        /// <summary>
        /// User's password has expired. Issue can be resolved by user during the interactive authentication flow.
        /// See <see href="https://aka.ms/msal-net-UiRequiredException">Understanding MsalUiRequiredException</see> for details.
        /// </summary>
        UserPasswordExpired,

        /// <summary>
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithPrompt(Prompt)"/> was used with a <c>Prompt.Never</c> value, 
        /// however this could not be honored by the server. Please use a different prompt behavior, such as <see cref="Prompt.SelectAccount"/>.
        /// See <see href="https://aka.ms/msal-net-UiRequiredException">Understanding MsalUiRequiredException</see> for details.
        /// </summary>
        PromptNeverFailed,

        /// <summary>
        /// An <see cref="AcquireTokenSilentParameterBuilder"/> call failed. This is usually part of the pattern 
        /// of calling <see cref="AcquireTokenSilentParameterBuilder"/> for getting a token from the cache, followed by an a different
        /// <c>AcquireToken</c> call for getting a token from Microsoft Entra ID. See the error message for details. 
        /// See <see href="https://aka.ms/msal-net-UiRequiredException">Understanding MsalUiRequiredException</see> for details.
        /// </summary>
        AcquireTokenSilentFailed
    }
}
