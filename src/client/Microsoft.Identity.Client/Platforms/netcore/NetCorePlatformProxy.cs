// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
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
        public NetCorePlatformProxy(ICoreLogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Get the user logged in
        /// </summary>
        public override Task<string> GetUserPrincipalNameAsync()
        {
            const int NameUserPrincipal = 8;
            return Task.FromResult(GetUserPrincipalName(NameUserPrincipal));
        }

        private string GetUserPrincipalName(int nameFormat)
        {
            if (IsWindowsPlatform())
            {
                uint userNameSize = 0;
                WindowsNativeMethods.GetUserNameEx(nameFormat, null, ref userNameSize);
                if (userNameSize == 0)
                {
                    throw new MsalClientException(
                        MsalError.GetUserNameFailed,
                        MsalErrorMessage.GetUserNameFailed,
                        new Win32Exception(Marshal.GetLastWin32Error()));
                }

                var sb = new StringBuilder((int)userNameSize);
                if (!WindowsNativeMethods.GetUserNameEx(nameFormat, sb, ref userNameSize))
                {
                    throw new MsalClientException(
                        MsalError.GetUserNameFailed,
                        MsalErrorMessage.GetUserNameFailed,
                        new Win32Exception(Marshal.GetLastWin32Error()));
                }

                return sb.ToString();
            }

            throw new PlatformNotSupportedException(
                "MSAL cannot determine the username (UPN) of the currently logged in user." +
                "For Integrated Windows Authentication and Username/Password flows, please use .WithUsername() before calling ExecuteAsync(). " +
                "For more details see https://aka.ms/msal-net-iwa");
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

        public override ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new InMemoryTokenCacheAccessor(Logger);
        }

        protected override IWebUIFactory CreateWebUiFactory() => new NetCoreWebUIFactory();
        protected override ICryptographyManager InternalGetCryptographyManager() => new NetCoreCryptographyManager();
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
        protected override IFeatureFlags CreateFeatureFlags() => new NetCoreFeatureFlags();

        public override Task StartDefaultOsBrowserAsync(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start msedge {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    try
                    {
                        Process.Start("xdg-open", url);
                    }
                    catch (Exception ex)
                    {
                        throw new MsalClientException(
                            MsalError.LinuxXdgOpen,
                            "Unable to open a web page using xdg-open. See inner exception for details. Possible causes for this error are: xdg-open is not installed or " +
                            "it cannot find a way to open an url - make sure you can open a web page by invoking from a terminal: xdg-open https://www.bing.com ",
                            ex);
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
                }
            }
            return Task.FromResult(0);

        }

        public override IPoPCryptoProvider GetDefaultPoPCryptoProvider()
        {
            return PoPProviderFactory.GetOrCreateProvider();
        }
        public override bool UseEmbeddedWebViewDefault => false;


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

        public override bool CanBrokerSupportSilentAuth()
        {
            return true;
        }

        public override bool BrokerSupportsWamAccounts => true;

    }
}
