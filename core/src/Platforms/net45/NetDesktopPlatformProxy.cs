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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Text;
using System.Security.Principal;
using Microsoft.Identity.Core.Platforms;
using Microsoft.Identity.Core.Cache;

namespace Microsoft.Identity.Core
{
    /// <summary>
    /// Platform / OS specific logic.
    /// </summary>
    internal class NetDesktopPlatformProxy : IPlatformProxy
    {
        private readonly bool _isMsal;

        public NetDesktopPlatformProxy(bool isMsal)
        {
            _isMsal = isMsal;
        }

        /// <summary>
        /// Get the user logged in to Windows or throws
        /// </summary>
        /// <returns>Upn or throws</returns>
        public async Task<string> GetUserPrincipalNameAsync()
        {
            // TODO: there is discrepancy between the implementation of this method on net45 - throws if upn not found - and uap and 
            // the rest of the platforms - returns "" 

            return await Task.Factory.StartNew(() =>
            {
                const int NameUserPrincipal = 8;
                uint userNameSize = 0;
                WindowsNativeMethods.GetUserNameEx(NameUserPrincipal, null, ref userNameSize);
                if (userNameSize == 0)
                {
                    throw CoreExceptionFactory.Instance.GetClientException(
                        CoreErrorCodes.GetUserNameFailed,
                        CoreErrorMessages.GetUserNameFailed,
                        new Win32Exception(Marshal.GetLastWin32Error()));
                }

                StringBuilder sb = new StringBuilder((int)userNameSize);
                if (!WindowsNativeMethods.GetUserNameEx(NameUserPrincipal, sb, ref userNameSize))
                {
                    throw CoreExceptionFactory.Instance.GetClientException(
                       CoreErrorCodes.GetUserNameFailed,
                       CoreErrorMessages.GetUserNameFailed,
                       new Win32Exception(Marshal.GetLastWin32Error()));
                }

                return sb.ToString();
            }).ConfigureAwait(false);
        }


        public async Task<bool> IsUserLocalAsync(RequestContext requestContext)
        {
            return await Task.Factory.StartNew(() =>
            {
                WindowsIdentity current = WindowsIdentity.GetCurrent();
                if (current != null)
                {
                    string prefix = WindowsIdentity.GetCurrent().Name.Split('\\')[0].ToUpperInvariant();
                    return prefix.Equals(Environment.MachineName.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }).ConfigureAwait(false);
        }

        public bool IsDomainJoined()
        {
            if (!IsWindows)
            {
                return false;
            }

            bool returnValue = false;
            try
            {
                WindowsNativeMethods.NetJoinStatus status;
                IntPtr pDomain;
                int result = WindowsNativeMethods.NetGetJoinInformation(null, out pDomain, out status);
                if (pDomain != IntPtr.Zero)
                {
                    WindowsNativeMethods.NetApiBufferFree(pDomain);
                }

                returnValue = result == WindowsNativeMethods.ErrorSuccess &&
                              status == WindowsNativeMethods.NetJoinStatus.NetSetupDomainName;
            }
            catch (Exception ex)
            {
                CoreLoggerBase.Default.WarningPii(ex);
                // ignore the exception as the result is already set to false;
            }

            return returnValue;
        }


        public string GetEnvironmentVariable(string variable)
        {
            string value = Environment.GetEnvironmentVariable(variable);
            return !string.IsNullOrWhiteSpace(value) ? value : null;
        }

        public string GetProcessorArchitecture()
        {
            if (IsWindows)
            {
                return WindowsNativeMethods.GetProcessorArchitecture();
            }
            else
            {
                return null;
            }
        }

        public string GetOperatingSystem()
        {
            return Environment.OSVersion.ToString();
        }

        public string GetDeviceModel()
        {
            // Since MSAL .NET may be used on servers, for security reasons, we do not emit device type.
            return null;
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
        public void ValidateRedirectUri(Uri redirectUri, RequestContext requestContext)
        {
            if (redirectUri == null)
            {
                throw new ArgumentNullException(nameof(redirectUri));
            }
        }

        /// <inheritdoc />
        public string GetRedirectUriAsString(Uri redirectUri, RequestContext requestContext)
        {
            return redirectUri.OriginalString;
        }

        /// <inheritdoc />
        public string GetDefaultRedirectUri(string correlationId)
        {
            return Constants.DefaultRedirectUri;
        }

        /// <inheritdoc />
        public string GetProductName()
        {
            return _isMsal ? "MSAL.Desktop" : "PCL.Desktop";
        }

        /// <inheritdoc />
        public ILegacyCachePersistence LegacyCachePersistence => new NetDesktopLegacyCachePersistence();

        /// <inheritdoc />
        public ITokenCacheAccessor TokenCacheAccessor => new TokenCacheAccessor();

        /// <inheritdoc />
        public ICryptographyManager CryptographyManager { get; } = new NetDesktopCryptographyManager();
    }
}
