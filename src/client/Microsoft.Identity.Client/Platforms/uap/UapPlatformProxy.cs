// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.uap.WamBroker;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.UI;
using Windows.ApplicationModel;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Security.Authentication.Web;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.System;

namespace Microsoft.Identity.Client.Platforms.uap
{
    /// <summary>
    /// Platform / OS specific logic. No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
    internal class UapPlatformProxy : AbstractPlatformProxy
    {
        public UapPlatformProxy(ILoggerAdapter logger)
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
                throw new MsalClientException(
                    MsalError.CannotAccessUserInformationOrUserNotDomainJoined,
                    MsalErrorMessage.UapCannotFindDomainUser);
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
                throw new MsalClientException(
                   MsalError.CannotAccessUserInformationOrUserNotDomainJoined,
                   MsalErrorMessage.UapCannotFindUpn);
            }

            // no domain, no upn -> missing User Info capability
            throw new MsalClientException(
                MsalError.CannotAccessUserInformationOrUserNotDomainJoined,
                MsalErrorMessage.UapCannotFindDomainUser);
        }

        protected override string InternalGetProcessorArchitecture()
        {
            return WindowsNativeMethods.GetProcessorArchitecture();
        }

        protected override string InternalGetOperatingSystem()
        {
            return "Windows 10";
        }

        protected override string InternalGetDeviceModel()
        {
            var deviceInformation = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
            return deviceInformation.SystemProductName;
        }
        public override bool BrokerSupportsWamAccounts => true;

        public override IBroker CreateBroker(ApplicationConfiguration appConfig, CoreUIParent uiParent)
        {
            return new WamBroker.WamBroker(uiParent, appConfig, Logger);
        }

        /// <inheritdoc/>
        public override string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false)
        {
            if (useRecommendedRedirectUri)
            {
                return WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();
            }
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

        public override ILegacyCachePersistence CreateLegacyCachePersistence() => new UapLegacyCachePersistence(Logger, CryptographyManager);

        public override ICacheSerializationProvider CreateTokenCacheBlobStorage() => 
            new SynchronizedAndEncryptedFileProvider(Logger);

        protected override IWebUIFactory CreateWebUiFactory() => new UapWebUIFactory();
        protected override ICryptographyManager InternalGetCryptographyManager() => new UapCryptographyManager();
        protected override IPlatformLogger InternalGetPlatformLogger() => new EventSourcePlatformLogger();

        protected override IFeatureFlags CreateFeatureFlags() => new UapFeatureFlags();
             
        public override bool LegacyCacheRequiresSerialization => false;
    }
}
