// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Win32;

namespace Microsoft.Identity.Client.Platforms.net45
{
    /// <summary>
    ///     Platform / OS specific logic.
    /// </summary>
    internal class NetDesktopPlatformProxy : AbstractPlatformProxy
    {
        /// <inheritdoc />
        public NetDesktopPlatformProxy(ILoggerAdapter logger)
            : base(logger)
        {
        }

        private bool IsWindows
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.Win32NT:
                    case PlatformID.WinCE:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <inheritdoc />
        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new InMemoryLegacyCachePersistance();
        }

        /// <inheritdoc />
        internal override string InternalGetDeviceModel()
        {
            // Since MSAL .NET may be used on servers, for security reasons, we do not emit device type.
            return null;
        }

        /// <inheritdoc />
        internal override string InternalGetOperatingSystem()
        {
            return DesktopOsHelper.GetWindowsVersionString();
        }

        /// <inheritdoc />
        internal override string InternalGetProcessorArchitecture()
        {
            return IsWindows ? WindowsNativeMethods.GetProcessorArchitecture() : null;
        }

        /// <inheritdoc />
        internal override string InternalGetCallingApplicationName()
        {
            // Considered PII, ensure that it is hashed.
            return Assembly.GetEntryAssembly()?.GetName()?.Name;
        }

        /// <inheritdoc />
        internal override string InternalGetCallingApplicationVersion()
        {
            // Considered PII, ensure that it is hashed.
            return Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();
        }

        /// <inheritdoc />
        internal override string InternalGetDeviceId()
        {
            try
            {
                // Considered PII, ensure that it is hashed.
                return NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                                .Select(nic => nic.GetPhysicalAddress()?.ToString()).FirstOrDefault();
            }
            catch (EntryPointNotFoundException)
            {
                // Thrown when ran in an Azure Runbook
                return null;
            }
        }

        /// <inheritdoc />
        internal override string InternalGetProductName()
        {
            return "MSAL.Desktop";
        }

        internal override string InternalGetRuntimeVersion()
        {
            // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#query-the-registry-using-code
            try
            {
                string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
                {
                    if (ndpKey?.GetValue("Release") != null)
                    {
                        int releaseKey = (int)ndpKey.GetValue("Release");
                        if (releaseKey >= 528040)
                        {
                            return "4.8 or later";
                        }

                        if (releaseKey >= 461808)
                        {
                            return "4.7.2";
                        }

                        if (releaseKey >= 461308)
                        {
                            return "4.7.1";
                        }

                        if (releaseKey >= 460798)
                        {
                            return "4.7";
                        }

                        if (releaseKey >= 394802)
                        {
                            return "4.6.2";
                        }

                        if (releaseKey >= 394254)
                        {
                            return "4.6.1";
                        }

                        if (releaseKey >= 393295)
                        {
                            return "4.6";
                        }

                        if (releaseKey >= 379893)
                        {
                            return "4.5.2";
                        }

                        if (releaseKey >= 378675)
                        {
                            return "4.5.1";
                        }

                        if (releaseKey >= 378389)
                        {
                            return "4.5";
                        }
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <inheritdoc />
        internal override ICryptographyManager InternalGetCryptographyManager() => new NetDesktopCryptographyManager();

        internal override IPlatformLogger InternalGetPlatformLogger() => new EventSourcePlatformLogger();

        internal override IFeatureFlags CreateFeatureFlags() => new NetDesktopFeatureFlags();

        public override IPoPCryptoProvider GetDefaultPoPCryptoProvider()
        {
            return PoPProviderFactory.GetOrCreateProvider();
        }

        public override IDeviceAuthManager CreateDeviceAuthManager()
        {
            return new NetDesktopDeviceAuthManager();
        }
    }
}
