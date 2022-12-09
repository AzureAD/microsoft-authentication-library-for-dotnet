// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.net45
{
    /// <summary>
    ///     Platform / OS specific logic.
    /// </summary>
    internal class NetDesktopPlatformProxyPublic : AbstractPlatformProxyPublic
    {
        // Instance variable because cannot extend multiple base classes.
        private readonly NetDesktopPlatformProxy _netDesktopPlatformProxy;

        /// <inheritdoc />
        public NetDesktopPlatformProxyPublic(ILoggerAdapter logger)
            : base(logger)
        {
            _netDesktopPlatformProxy = new NetDesktopPlatformProxy(logger);
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
            return _netDesktopPlatformProxy.CreateLegacyCachePersistence();
        }

        /// <inheritdoc />
        protected override IWebUIFactory CreateWebUiFactory()
        {
            return new NetDesktopWebUIFactory();
        }

        /// <inheritdoc />
        internal override string InternalGetDeviceModel()
        {
            // Since MSAL .NET may be used on servers, for security reasons, we do not emit device type.
            return _netDesktopPlatformProxy.InternalGetDeviceModel();
        }

        /// <inheritdoc />
        internal override string InternalGetOperatingSystem()
        {
            return _netDesktopPlatformProxy.InternalGetOperatingSystem();
        }

        /// <inheritdoc />
        internal override string InternalGetProcessorArchitecture()
        {
            return _netDesktopPlatformProxy.InternalGetProcessorArchitecture();
        }

        /// <inheritdoc />
        internal override string InternalGetCallingApplicationName()
        {
            // Considered PII, ensure that it is hashed.
            return _netDesktopPlatformProxy.InternalGetCallingApplicationName();
        }

        /// <inheritdoc />
        internal override string InternalGetCallingApplicationVersion()
        {
            // Considered PII, ensure that it is hashed.
            return _netDesktopPlatformProxy.InternalGetCallingApplicationVersion();
        }

        /// <inheritdoc />
        internal override string InternalGetDeviceId()
        {
            // Considered PII, ensure that it is hashed.
            return _netDesktopPlatformProxy.InternalGetDeviceId();
        }

        /// <inheritdoc />
        internal override string InternalGetProductName()
        {
            return _netDesktopPlatformProxy.InternalGetProductName();
        }

        internal override string InternalGetRuntimeVersion()
        {
            // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#query-the-registry-using-code
            return _netDesktopPlatformProxy.InternalGetRuntimeVersion();
        }

        /// <inheritdoc />
        internal override ICryptographyManager InternalGetCryptographyManager() => _netDesktopPlatformProxy.InternalGetCryptographyManager();

        internal override IPlatformLogger InternalGetPlatformLogger() => _netDesktopPlatformProxy.InternalGetPlatformLogger();

        internal override IFeatureFlags CreateFeatureFlags() => _netDesktopPlatformProxy.CreateFeatureFlags();

        public override Task StartDefaultOsBrowserAsync(string url, bool isBrokerConfigured)
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
            return _netDesktopPlatformProxy.GetDefaultPoPCryptoProvider();
        }

        public override IDeviceAuthManager CreateDeviceAuthManager()
        {
            return _netDesktopPlatformProxy.CreateDeviceAuthManager();
        }

        public override bool BrokerSupportsWamAccounts => true;

    }
}
