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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.System;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using String = System.String;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Windows.Foundation.Collections;

namespace Microsoft.Identity.Client.Platforms.uap
{
    /// <summary>
    /// Platform / OS specific logic. No library (ADAL / MSAL) specific code should go in here. 
    /// </summary>
    internal class UapPlatformProxy : AbstractPlatformProxy
    {
        public UapPlatformProxy(ICoreLogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Get the user logged in to Windows or throws
        /// </summary>
        /// <remarks>
        /// Win10 allows several identities to be logged in at once; 
        /// select the first principal name that can be used
        /// </remarks>
        /// <returns>The username or throws</returns>
        public override async Task<string> GetUserPrincipalNameAsync()
        {
            IReadOnlyList<User> users = await User.FindAllAsync();
            if (users == null || !users.Any())
            {
                throw MsalExceptionFactory.GetClientException(
                    CoreErrorCodes.CannotAccessUserInformationOrUserNotDomainJoined,
                    CoreErrorMessages.UapCannotFindDomainUser);
            }

            var getUserDetailTasks = users.Select(async u =>
            {
                object domainObj = await u.GetPropertyAsync(KnownUserProperties.DomainName);
                string domainString = domainObj?.ToString();

                object principalObject = await u.GetPropertyAsync(KnownUserProperties.PrincipalName);
                string principalNameString = principalObject?.ToString();

                return new { Domain = domainString, PrincipalName = principalNameString };
            }).ToList();

            var userDetails = await Task.WhenAll(getUserDetailTasks).ConfigureAwait(false);

            // try to get a user that has both domain name and upn
            var userDetailWithDomainAndPn = userDetails.FirstOrDefault(
                d => !string.IsNullOrWhiteSpace(d.Domain) &&
                !string.IsNullOrWhiteSpace(d.PrincipalName));

            if (userDetailWithDomainAndPn != null)
            {
                return userDetailWithDomainAndPn.PrincipalName;
            }

            // try to get a user that at least has upn
            var userDetailWithPn = userDetails.FirstOrDefault(
              d => !string.IsNullOrWhiteSpace(d.PrincipalName));

            if (userDetailWithPn != null)
            {
                return userDetailWithPn.PrincipalName;
            }

            // user has domain name, but no upn -> missing Enterprise Auth capability
            if (userDetails.Any(d => !string.IsNullOrWhiteSpace(d.Domain)))
            {
                throw MsalExceptionFactory.GetClientException(
                   CoreErrorCodes.CannotAccessUserInformationOrUserNotDomainJoined,
                   CoreErrorMessages.UapCannotFindUpn);
            }

            // no domain, no upn -> missing User Info capability
            throw MsalExceptionFactory.GetClientException(
                CoreErrorCodes.CannotAccessUserInformationOrUserNotDomainJoined,
                CoreErrorMessages.UapCannotFindDomainUser);

        }

        public override async Task<bool> IsUserLocalAsync(RequestContext requestContext)
        {
            IReadOnlyList<User> users = await User.FindAllAsync();
            return users.Any(u => u.Type == UserType.LocalUser || u.Type == UserType.LocalGuest);
        }

        public override bool IsDomainJoined()
        {
            return NetworkInformation.GetHostNames().Any(entry => entry.Type == HostNameType.DomainName);
        }

        public override string GetEnvironmentVariable(string variable)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return localSettings.Values.ContainsKey(variable) ? localSettings.Values[variable].ToString() : null;
        }

        protected override string InternalGetProcessorArchitecture()
        {
            return WindowsNativeMethods.GetProcessorArchitecture();
        }

        protected override string InternalGetOperatingSystem()
        {
            // In WinRT, there is no way to reliably get OS version. All can be done reliably is to check 
            // for existence of specific features which does not help in this case, so we do not emit OS in WinRT.
            return null;
        }

        protected override string InternalGetDeviceModel()
        {
            var deviceInformation = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
            return deviceInformation.SystemProductName;
        }

        /// <inheritdoc />
        public override string GetBrokerOrRedirectUri(Uri redirectUri)
        {
            return redirectUri.OriginalString;
        }

        /// <inheritdoc />
        public override string GetDefaultRedirectUri(string correlationId)
        {
            return Constants.DefaultRedirectUri;
        }

        protected override string InternalGetProductName()
        {
            return "MSAL.UAP";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Name of the calling application</returns>
        protected override string InternalGetCallingApplicationName()
        {
            return Package.Current?.DisplayName?.ToString();
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Version of the calling application</returns>
        protected override string InternalGetCallingApplicationVersion()
        {
            return Package.Current?.Id?.Version.ToString();
        }

        /// <summary>
        /// Considered PII. Please ensure that it is hashed. 
        /// </summary>
        /// <returns>Device identifier</returns>
        protected override string InternalGetDeviceId()
        {
            return new EasClientDeviceInformation()?.Id.ToString();
        }

        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new UapLegacyCachePersistence(Logger, CryptographyManager);
        }

        public override ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new InMemoryTokenCacheAccessor();
        }

        public override ITokenCacheBlobStorage CreateTokenCacheBlobStorage()
        {
            return new UapTokenCacheBlobStorage(CryptographyManager, Logger);
        }

        protected override IWebUIFactory CreateWebUiFactory() => new WebUIFactory();
        protected override ICryptographyManager InternalGetCryptographyManager() => new UapCryptographyManager();
        protected override IPlatformLogger InternalGetPlatformLogger() => new EventSourcePlatformLogger();

    }
}
