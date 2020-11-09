// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NET_CORE || NETSTANDARD

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Interfaces;
using Microsoft.Identity.Client.Internal.Shared;
using Microsoft.Identity.Client.Shared.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore.Internal;

namespace Microsoft.Identity.Client.Platforms.netstandardcore
{
    /// <summary>
    /// Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
    internal class NetPlatformProxy : AbstractSharedPlatformProxy
    {
        public NetPlatformProxy(ICoreLogger logger)
            : base(logger)
        {
        }

        protected override string InternalGetProcessorArchitecture()
        {
            return null;
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
#if NET_CORE
            return "MSAL.NetCore";
#elif NETSTANDARD
            return "MSAL.CoreCLR";
#endif
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

        protected override IPlatformLogger InternalGetPlatformLogger() => new EventSourcePlatformLogger();

        public override string GetDeviceNetworkState()
        {
            // TODO(mats):
            return string.Empty;
        }

        public override string GetDevicePlatformTelemetryId()
        {
            // TODO(mats):
            return string.Empty;
        }

        public override string GetMatsOsPlatform()
        {
            // TODO(mats): need to detect operating system and switch on it to determine proper enum
            return MatsConverter.AsString(OsPlatform.Win32);
        }

        public override int GetMatsOsPlatformCode()
        {
            // TODO(mats): need to detect operating system and switch on it to determine proper enum
            return MatsConverter.AsInt(OsPlatform.Win32);
        }

        public override bool BrokerSupportsWamAccounts => true;

        public override RuntimePlatform RuntimePlatform => RuntimePlatform.NetCore;

        public override IPoPCryptoProvider GetDefaultPoPCryptoProvider()
        {
            return PoPProviderFactory.GetOrCreateProvider();
        }

        /// <summary>
        ///  Is this a windows platform
        /// </summary>
        /// <returns>A  value indicating if we are running on windows or not</returns>
        public static bool IsWindowsPlatform()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }

        /// <summary>
        /// Is this a MAC platform
        /// </summary>
        /// <returns>A value indicating if we are running on mac or not</returns>
        public static bool IsMacPlatform()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        /// <summary>
        /// Is this a linux platform
        /// </summary>
        /// <returns>A  value indicating if we are running on linux or not</returns>
        public static bool IsLinuxPlatform()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix;
        }

        protected override IFeatureFlags CreateFeatureFlags()
        {
            return new NetFeatureFlags();
        }

        protected override ICryptographyManager InternalGetCryptographyManager()
        {
            return new NetCryptographyManager();
        }
    }
}
#endif
