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

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// This exception class is to inform developers that UI interaction is required for authentication to 
    /// succeed. It's thrown when calling <see cref="ClientApplicationBase.AcquireTokenSilentAsync(System.Collections.Generic.IEnumerable{string}, IAccount)"/> or one
    /// of its overrides, and when the token does not exists in the cache, or the user needs to provide more content, or perform multiple factor authentication based
    /// on Azure AD policies, etc..
    /// For more details, see https://aka.ms/msal-net-exceptions
    /// </summary>
    public class MsalUiRequiredException : MsalServiceException
    {
        /// <summary>
        /// Standard OAuth2 protocol error code. It indicates to the libray that the application needs to expose the UI to the user  
        /// so that the user does an interactive action in order to get a new token.
        /// <para>Mitigation:</para> If your application is a <see cref="T:PublicClientApplication"/> call one of the <c>AcquireTokenAsync</c> overrides to 
        /// perform an interactive authentication. If your application is a <see cref="T:ConfidentialClientApplication"/> chances are that the Claims member
        /// of the exception is not empty. See <see cref="P:MsalServiceException.Claims"/> for the right mitigation
        /// </summary>
        public const string InvalidGrantError = "invalid_grant";

#if !DESKTOP && !NET_CORE
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
        /// <summary>
        /// <para>Mitigation:</para> If your application is a <see cref="PublicClientApplication"/> call one of the <c>AcquireTokenAsync</c> overrides so
        /// that the user of your application signs-in and accepts consent. If your application is a <see cref="T:ConfidentialClientApplication"/>. If it's a Web App
        /// you should have previously called <see cref="ConfidentialClientApplication.AcquireTokenByAuthorizationCodeAsync(string, System.Collections.Generic.IEnumerable{string})"/>
        /// as described in https://aka.ms/msal-net-authorization-code. This error should not happen in Web APIs.
        /// </summary>
        public const string NoTokensFoundError = "no_tokens_found";
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        /// This error code comes back from <see cref="ClientApplicationBase.AcquireTokenSilentAsync(System.Collections.Generic.IEnumerable{string}, IAccount)"/> calls when a null user is 
        /// passed as the <c>account</c> parameter.
        /// </summary>
        public const string UserNullError = "user_null";

        /// <summary>
        /// This error code denotes that no account was found having the given login hint.
        /// </summary>
        public const string NoAccountForLoginHint = "no_account_for_login_hint";

        /// <summary>
        /// This error code denotes that multiple accounts were found having the same login hint and MSAL 
        /// cannot chose one. Please use the overload of AcquireTokenSilent where you pass an account.
        /// </summary>
        public const string MultipleAccountsForLoginHint = "multiple_accounts_for_login_hint";


        /// <summary>
        /// This error code comes back from <see cref="ClientApplicationBase.AcquireTokenSilentAsync(System.Collections.Generic.IEnumerable{string}, IAccount)"/> calls when 
        /// the user cache had not been set in the application constructor.
        /// </summary>
        public const string TokenCacheNullError = "token_cache_null";

        // TODO(migration):  Prompt.Never no longer exists.  Validate removing this error message.
        /// <summary>
        /// One of two conditions was encountered:
        /// <list type="bullet">
        /// <item><description>The <c>Prompt.Never</c> UI behavior was passed in an interactive token call, but the constraint could not be honored because user interaction is required,
        /// for instance because the user needs to re-sign-in, give consent for more scopes, or perform multiple factor authentication.
        /// </description></item>
        /// <item><description>
        /// An error occurred during a silent web authentication that prevented the authentication flow from completing in a short enough time frame.
        /// </description></item>
        /// </list>
        /// <para>Remediation:</para>call one of the <c>AcquireTokenAsync</c> overrides so that the user of your application signs-in and accepts consent. 
        /// </summary>
        public const string NoPromptFailedError = "no_prompt_failed";

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code and error message.
        /// </summary>
        /// <param name="errorCode">
        /// The error code returned by the service or generated by the client. This is the code you can rely on
        /// for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        public MsalUiRequiredException(string errorCode, string errorMessage) : 
            this(errorCode, errorMessage, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code, error message and inner exception indicating the root cause.
        /// </summary>
        /// <param name="errorCode">
        /// The error code returned by the service or generated by the client. This is the code you can rely on
        /// for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">Represents the root cause of the exception.</param>
        public MsalUiRequiredException(string errorCode, string errorMessage, Exception innerException) : base(errorCode, errorMessage, innerException)
        {
        }
    }
}
