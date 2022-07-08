// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Mac
{
    /// <summary>
    /// Platform / OS specific logic.
    /// </summary>
    internal class MacPlatformProxy : AbstractPlatformProxy
    {
        internal const string IosDefaultRedirectUriTemplate = "msal{0}://auth";

        public MacPlatformProxy(ILoggerAdapter logger)
            : base(logger)
        {
        }

        /// <summary>
        ///     Get the user logged
        /// </summary>
        public override Task<string> GetUserPrincipalNameAsync()
        {
            return Task.FromResult(string.Empty);
        }

        protected override string InternalGetProcessorArchitecture()
        {
            return null;
        }

        protected override string InternalGetOperatingSystem()
        {
            return Environment.OSVersion.ToString();
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
                return Constants.NativeClientRedirectUri;
            }

            return Constants.DefaultRedirectUri;
        }

        protected override string InternalGetProductName()
        {
            return "MSAL.Xamarin.Mac";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Name of the calling application</returns>
        protected override string InternalGetCallingApplicationName()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Name;
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Version of the calling application</returns>
        protected override string InternalGetCallingApplicationVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();
        }

        private static readonly Lazy<string> DeviceIdLazy = new Lazy<string>(
           () => NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                                 .Select(nic => nic.GetPhysicalAddress()?.ToString()).FirstOrDefault());

        /// <summary>
        /// Considered PII. Please ensure that it is hashed.
        /// </summary>
        /// <returns>Device identifier</returns>
        protected override string InternalGetDeviceId()
        {
            return DeviceIdLazy.Value;
        }

        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            // There is no ADAL for MAC
            return new NullLegacyCachePersistence();
        }

        protected override IWebUIFactory CreateWebUiFactory() => new MacUIFactory();
        protected override ICryptographyManager InternalGetCryptographyManager() => new CommonCryptographyManager();
        protected override IPlatformLogger InternalGetPlatformLogger() => new ConsolePlatformLogger();

        protected override IFeatureFlags CreateFeatureFlags() => new MacFeatureFlags();

        public override Task StartDefaultOsBrowserAsync(string url, bool isBrokerConfigured)
        {
            Process.Start("open", url);
            return Task.FromResult(0);
        }
    }
}
