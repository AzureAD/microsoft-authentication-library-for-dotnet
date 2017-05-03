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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Component to be used for native applications (Desktop/UWP/iOS/Android).
    /// </summary>
    public interface IPublicClientApplication : IClientApplicationBase
    {

#if WINRT
        /// <summary>
        /// 
        /// </summary>
        bool UseCorporateNetwork { get; set; }
#endif

        // expose the interactive API without UIParent only for platforms that 
        // do not need it to operate like desktop, UWP, iOS.
#if !ANDROID
        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scope);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IUser user);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IUser user,
            UIBehavior behavior,
            string extraQueryParameters);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent, string authority);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IUser user,
            UIBehavior behavior,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority);

#endif
        
        // these API methods are exposed on other platforms.
        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IUser user, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IUser user,
            UIBehavior behavior,
            string extraQueryParameters, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent, string authority, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IUser user,
            UIBehavior behavior,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority, UIParent parent);
    }
}