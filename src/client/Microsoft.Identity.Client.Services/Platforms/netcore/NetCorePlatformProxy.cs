// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.Platforms.Shared.NetStdCore;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netcore
{
    /// <summary>
    /// Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
    internal class NetCorePlatformProxy : AbstractPlatformProxy
    {
        public NetCorePlatformProxy(ILoggerAdapter logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Get the user logged in
        /// </summary>
        public override Task<string> GetUserPrincipalNameAsync()
        {
            throw new NotSupportedException(MsalErrorMessage.PublicClientOnlyOperation);
        }

        protected override string InternalGetProcessorArchitecture()
        {
            return DesktopOsHelper.IsWindows() ? WindowsNativeMethods.GetProcessorArchitecture() : null;
        }

        protected override string InternalGetOperatingSystem()
        {
            return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        }

        protected override string InternalGetDeviceModel()
        {
            return null;
        }

        /// <inheritdoc />
        public override string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false)
        {
            if (useRecommendedRedirectUri)
            {
                return Constants.LocalHostRedirectUri;
            }

            return Constants.DefaultRedirectUri;
        }

        protected override string InternalGetProductName()
        {
            return "MSAL.NetCore";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Name of the calling application</returns>
        protected override string InternalGetCallingApplicationName()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Name?.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Version of the calling application</returns>
        protected override string InternalGetCallingApplicationVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();
        }

        /// <summary>
        /// Considered PII. Please ensure that it is hashed.
        /// </summary>
        /// <returns>Device identifier</returns>
        protected override string InternalGetDeviceId()
        {
            return Environment.MachineName;
        }

        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new InMemoryLegacyCachePersistance();
        }

        protected override IWebUIFactory CreateWebUiFactory() => throw new NotSupportedException(MsalErrorMessage.PublicClientOnlyOperation);
        protected override ICryptographyManager InternalGetCryptographyManager() => new CommonCryptographyManager();
        protected override IPlatformLogger InternalGetPlatformLogger() => new EventSourcePlatformLogger();

        protected override IFeatureFlags CreateFeatureFlags() => new NetCoreFeatureFlags();

        public override Task StartDefaultOsBrowserAsync(string url, bool isBrokerConfigured)
        {
            throw new NotSupportedException(MsalErrorMessage.PublicClientOnlyOperation);
        }

        public override IPoPCryptoProvider GetDefaultPoPCryptoProvider()
        {
            return PoPProviderFactory.GetOrCreateProvider();
        }

        public override bool BrokerSupportsWamAccounts => throw new NotSupportedException(MsalErrorMessage.PublicClientOnlyOperation);

        public override IDeviceAuthManager CreateDeviceAuthManager() => new NetCoreDeviceAuthManager();
    }
}
