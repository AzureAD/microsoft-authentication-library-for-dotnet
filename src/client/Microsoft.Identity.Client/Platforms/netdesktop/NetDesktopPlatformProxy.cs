// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using Microsoft.Win32;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.net45.Http;
using Microsoft.Identity.Client.PlatformsCommon;

namespace Microsoft.Identity.Client.Platforms.net45
{
    /// <summary>
    ///     Platform / OS specific logic.
    /// </summary>
    internal class NetDesktopPlatformProxy : AbstractPlatformProxy
    {
        /// <inheritdoc />
        public NetDesktopPlatformProxy(ICoreLogger logger)
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

        /// <summary>
        ///     Get the user logged in to Windows or throws
        /// </summary>
        /// <returns>Upn or throws</returns>
        public override Task<string> GetUserPrincipalNameAsync()
        {
            const int NameUserPrincipal = 8;
            return Task.FromResult(GetUserPrincipalName(NameUserPrincipal));
        }

        private string GetUserPrincipalName(int nameFormat)
        {
            // TODO: there is discrepancy between the implementation of this method on net45 - throws if upn not found - and uap and
            // the rest of the platforms - returns ""

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

        public override Task<bool> IsUserLocalAsync(RequestContext requestContext)
        {
            var current = WindowsIdentity.GetCurrent();
            if (current != null)
            {
                string prefix = WindowsIdentity.GetCurrent().Name.Split('\\')[0].ToUpperInvariant();
                return Task.FromResult(
                    prefix.Equals(Environment.MachineName.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(false);
        }

        public override bool IsDomainJoined()
        {
            if (!IsWindows)
            {
                return false;
            }

            bool returnValue = false;
            try
            {
                int result = WindowsNativeMethods.NetGetJoinInformation(null, out var pDomain, out var status);
                if (pDomain != IntPtr.Zero)
                {
                    WindowsNativeMethods.NetApiBufferFree(pDomain);
                }

                returnValue = result == WindowsNativeMethods.ErrorSuccess &&
                              status == WindowsNativeMethods.NetJoinStatus.NetSetupDomainName;
            }
            catch (Exception ex)
            {
                Logger.WarningPii(ex);
                // ignore the exception as the result is already set to false;
            }

            return returnValue;
        }

        public override string GetEnvironmentVariable(string variable)
        {
            if (string.IsNullOrWhiteSpace(variable))
            {
                throw new ArgumentNullException(nameof(variable));
            }

            return Environment.GetEnvironmentVariable(variable);
        }

        /// <inheritdoc />
        public override string GetBrokerOrRedirectUri(Uri redirectUri)
        {
            return redirectUri.OriginalString;
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

        /// <inheritdoc />
        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new InMemoryLegacyCachePersistance();
        }

        /// <inheritdoc />
        public override ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new InMemoryTokenCacheAccessor(Logger);
        }

        /// <inheritdoc />
        protected override IWebUIFactory CreateWebUiFactory()
        {
            return new NetDesktopWebUIFactory();
        }

        /// <inheritdoc />
        protected override string InternalGetDeviceModel()
        {
            // Since MSAL .NET may be used on servers, for security reasons, we do not emit device type.
            return null;
        }

        /// <inheritdoc />
        protected override string InternalGetOperatingSystem()
        {
            return Environment.OSVersion.ToString();
        }

        /// <inheritdoc />
        protected override string InternalGetProcessorArchitecture()
        {
            return IsWindows ? WindowsNativeMethods.GetProcessorArchitecture() : null;
        }

        /// <inheritdoc />
        protected override string InternalGetCallingApplicationName()
        {
            // Considered PII, ensure that it is hashed.
            return Assembly.GetEntryAssembly()?.GetName()?.Name;
        }

        /// <inheritdoc />
        protected override string InternalGetCallingApplicationVersion()
        {
            // Considered PII, ensure that it is hashed.
            return Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();
        }

        /// <inheritdoc />
        protected override string InternalGetDeviceId()
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
        protected override string InternalGetProductName()
        {
            return "MSAL.Desktop";
        }

        /// <inheritdoc />
        protected override ICryptographyManager InternalGetCryptographyManager() => new NetDesktopCryptographyManager();

        public override string GetDeviceNetworkState()
        {
            // TODO(mats):
            return string.Empty;
        }

        public override string GetDevicePlatformTelemetryId()
        {
            const int NameSamCompatible = 2;

            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\SQMClient", false);
            object val = key.GetValue("MachineId");
            if (val == null)
            {
                return string.Empty;
            }

            string win32DeviceId = val.ToString();

            string userName = GetUserPrincipalName(NameSamCompatible);

            // NameSamCompatible might include an email address. remove the domain before hashing.
            int atIdx = userName.IndexOf('@');
            if (atIdx >= 0)
            {
                userName = userName.Substring(0, atIdx);
            }

            string unhashedDpti = win32DeviceId + userName;

            var hashedBytes = InternalGetCryptographyManager().CreateSha256HashBytes(unhashedDpti);
            var sb = new StringBuilder();

            foreach (var b in hashedBytes)
            {
                sb.Append($"{b:x2}");
            }

            string dptiOutput = sb.ToString();
            return dptiOutput;
        }

        public override string GetMatsOsPlatform()
        {
            return MatsConverter.AsString(OsPlatform.Win32);
        }

        public override int GetMatsOsPlatformCode()
        {
            return MatsConverter.AsInt(OsPlatform.Win32);
        }
        protected override IPlatformLogger InternalGetPlatformLogger() => new EventSourcePlatformLogger();

        protected override IFeatureFlags CreateFeatureFlags() => new NetDesktopFeatureFlags();

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
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }

            return Task.FromResult(0);
        }

        public override IPoPCryptoProvider GetDefaultPoPCryptoProvider()
        {
            return new NetSharedPoPCryptoProvider();
        }

        public override IDeviceAuthManager CreateDeviceAuthManager()
        {
            return new NetDesktopDeviceAuthManager();
        }

        public override IMsalHttpClientFactory CreateDefaultHttpClientFactory()
        {
            return new NetDesktopHttpClientFactory();
        }
    }
}
