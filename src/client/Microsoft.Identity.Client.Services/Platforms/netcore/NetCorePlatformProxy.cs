// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Reflection;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

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

        internal override string InternalGetProcessorArchitecture()
        {
            return DesktopOsHelper2.IsWindows() ? WindowsNativeMethods.GetProcessorArchitecture() : null;
        }

        internal override string InternalGetOperatingSystem()
        {
            return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        }

        internal override string InternalGetDeviceModel()
        {
            return null;
        }

        internal override string InternalGetProductName()
        {
            return "MSAL.NetCore";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Name of the calling application</returns>
        internal override string InternalGetCallingApplicationName()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Name?.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Version of the calling application</returns>
        internal override string InternalGetCallingApplicationVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();
        }

        /// <summary>
        /// Considered PII. Please ensure that it is hashed.
        /// </summary>
        /// <returns>Device identifier</returns>
        internal override string InternalGetDeviceId()
        {
            return Environment.MachineName;
        }

        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new InMemoryLegacyCachePersistance();
        }
        internal override ICryptographyManager InternalGetCryptographyManager() => new CommonCryptographyManager();
        internal override IPlatformLogger InternalGetPlatformLogger() => new EventSourcePlatformLogger();

        internal override IFeatureFlags CreateFeatureFlags() => new NetCoreFeatureFlags();

        public override IPoPCryptoProvider GetDefaultPoPCryptoProvider()
        {
            return PoPProviderFactory.GetOrCreateProvider();
        }

        public override IDeviceAuthManager CreateDeviceAuthManager() => new NetCoreDeviceAuthManager();
    }
}
