//------------------------------------------------------------------------------
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
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security;
using Microsoft.Identity.Client.ApiConfig.Executors;

namespace Microsoft.Identity.Client
{
#if !DESKTOP && !NET_CORE
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
    /// <summary>
    /// Class to be used to acquire tokens in desktop or mobile applications (Desktop / UWP / Xamarin.iOS / Xamarin.Android).
    /// public client applications are not trusted to safely keep application secrets, and therefore they only access Web APIs in the name of the user only
    /// (they only support public client flows). For details see https://aka.ms/msal-net-client-applications
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Contrary to <see cref="Microsoft.Identity.Client.ConfidentialClientApplication"/>, public clients are unable to hold configuration time secrets,
    /// and as a result have no client secret</description></item>
    /// <item><description>the redirect URL is pre-proposed by the library. It does not need to be passed in the constructor</description></item>
    /// <item><description>.NET Core does not support UI, and therefore this platform does not provide the interactive token acquisition methods</description></item>
    /// </list>
    /// </remarks>
    public sealed partial class PublicClientApplication : ClientApplicationBase, IPublicClientApplication, IByRefreshToken
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        internal PublicClientApplication(ApplicationConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSystemWebViewAvailable
        {
            get => ServiceBundle.PlatformProxy.IsSystemWebViewAvailable;
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will be required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.
        /// 
        /// You can also pass optional parameters by calling:
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithPrompt(Prompt)"/> to specify the user experience
        /// when signing-in, <see cref="AcquireTokenInteractiveParameterBuilder.WithUseEmbeddedWebView(bool)"/> to specify
        /// if you want to use the embedded web browser or the system default browser,
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithAccount(IAccount)"/> or <see cref="AcquireTokenInteractiveParameterBuilder.WithLoginHint(string)"/>
        /// to prevent the select account dialog from appearing in the case you want to sign-in a specific account,
        /// WithParentActivityOrWindow to optimize how the browser is shown, e.g.
        /// for centering the browser window on the app window. Required on Xamarin.Android
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithExtraScopesToConsent(IEnumerable{string})"/> if you want to let the
        /// user pre-consent to additional scopes (which won't be returned in the access token),
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction
        /// </remarks>
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder AcquireTokenInteractive(
            IEnumerable<string> scopes)
        {
#if NET_CORE
            throw new PlatformNotSupportedException("On .NET Core, interactive authentication is not supported. " + 
                "Consider using Device Code Flow https://aka.ms/msal-net-device-code-flow or Integrated Windows Auth https://aka.ms/msal-net-iwa");
#else
            return AcquireTokenInteractiveParameterBuilder.Create(
                ClientExecutorFactory.CreatePublicClientExecutor(this),
                scopes);
#endif
        }

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on
        /// another device. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>The method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>
        /// You can also pass optional parameters by calling:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction.
        /// </remarks>
        public AcquireTokenWithDeviceCodeParameterBuilder AcquireTokenWithDeviceCode(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            return AcquireTokenWithDeviceCodeParameterBuilder.Create(
                ClientExecutorFactory.CreatePublicClientExecutor(this),
                scopes,
                deviceCodeResultCallback);
        }

        AcquireTokenByRefreshTokenParameterBuilder IByRefreshToken.AcquireTokenByRefreshToken(
            IEnumerable<string> scopes,
            string refreshToken)
        {
            return AcquireTokenByRefreshTokenParameterBuilder.Create(
                ClientExecutorFactory.CreateClientApplicationBaseExecutor(this),
                scopes,
                refreshToken);
        }

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows,
        /// via Integrated Windows Authentication. See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>
        /// You can also pass optional parameters by calling:
        /// <see cref="AcquireTokenByIntegratedWindowsAuthParameterBuilder.WithUsername(string)"/> to pass the identifier
        /// of the user account for which to acquire a token with Integrated Windows authentication. This is generally in
        /// UserPrincipalName (UPN) format, e.g. john.doe@contoso.com. This is normally not needed, but some Windows administrators
        /// set policies preventing applications from looking-up the signed-in user in Windows, and in that case the username
        /// needs to be passed.
        /// You can also chain with
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction.
        /// </remarks>
        public AcquireTokenByIntegratedWindowsAuthParameterBuilder AcquireTokenByIntegratedWindowsAuth(
            IEnumerable<string> scopes)
        {
            return AcquireTokenByIntegratedWindowsAuthParameterBuilder.Create(
                ClientExecutorFactory.CreatePublicClientExecutor(this),
                scopes);
        }

        /// <summary>
        /// Non-interactive request to acquire a security token from the authority, via Username/Password Authentication.
        /// Available only on .net desktop and .net core. See https://aka.ms/msal-net-up for details.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="password">User password as a secure string.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can also pass optional parameters by chaining the builder with:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass 
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction.
        /// </remarks>
        public AcquireTokenByUsernamePasswordParameterBuilder AcquireTokenByUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            SecureString password)
        {
            return AcquireTokenByUsernamePasswordParameterBuilder.Create(
                ClientExecutorFactory.CreatePublicClientExecutor(this),
                scopes,
                username,
                password);
        }
    }
}
