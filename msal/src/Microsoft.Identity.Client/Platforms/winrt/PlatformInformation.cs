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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.System.UserProfile;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;

namespace Microsoft.Identity.Client
{
    internal class PlatformInformation : PlatformInformationBase
    {
        public override string GetProductName()
        {
            return "MSAL.WinRT";
        }

        public override string GetEnvironmentVariable(string variable)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return localSettings.Values.ContainsKey(variable) ? localSettings.Values[variable].ToString() : null;
        }

        public override string GetProcessorArchitecture()
        {
            return NativeMethods.GetProcessorArchitecture();
        }

        public override string GetOperatingSystem()
        {
            // In WinRT, there is no way to reliably get OS version. All can be done reliably is to check 
            // for existence of specific features which does not help in this case, so we do not emit OS in WinRT.
            return null;
        }

        public override string GetDeviceModel()
        {
            var deviceInformation = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
            return deviceInformation.SystemProductName;
        }

        public override async Task<bool> IsUserLocalAsync(RequestContext requestContext)
        {
            if (!UserInformation.NameAccessAllowed)
            {
                // The access is not allowed and we cannot determine whether this is a local user or not. So, we do NOT add form auth parameter.
                // This is the case where we can advise customers to add extra query parameter if they want.

                const string msg =
                    "Cannot access user information to determine whether it is a local user or not due to machine's privacy setting.";
                requestContext.Logger.Info(msg);
                requestContext.Logger.InfoPii(msg);
                return false;
            }

            try
            {
                return string.IsNullOrEmpty(await UserInformation.GetDomainNameAsync().AsTask().ConfigureAwait(false));
            }
            catch (UnauthorizedAccessException ae)
            {
                requestContext.Logger.Warning(ae.Message);
                const string msg = "Cannot try Windows Integrated Auth due to lack of Enterprise capability.";
                requestContext.Logger.Info(msg);
                requestContext.Logger.InfoPii(msg);
                // This mostly means Enterprise capability is missing, so WIA cannot be used and
                // we return true to add form auth parameter in the caller.
                return true;
            }
        }

        public override bool IsDomainJoined()
        {
            return NetworkInformation.GetHostNames().Any(entry => entry.Type == HostNameType.DomainName);
        }

        public override string GetRedirectUriAsString(Uri redirectUri, RequestContext requestContext)
        {
            return ReferenceEquals(redirectUri, Constants.SsoPlaceHolderUri)
                ? WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString
                : redirectUri.OriginalString;
        }

        private static class NativeMethods
        {
            private const int PROCESSOR_ARCHITECTURE_AMD64 = 9;
            private const int PROCESSOR_ARCHITECTURE_ARM = 5;
            private const int PROCESSOR_ARCHITECTURE_IA64 = 6;
            private const int PROCESSOR_ARCHITECTURE_INTEL = 0;

            [DllImport("kernel32.dll")]
            private static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

            public static string GetProcessorArchitecture()
            {
                try
                {
                    SYSTEM_INFO systemInfo = new SYSTEM_INFO();
                    GetNativeSystemInfo(ref systemInfo);
                    switch (systemInfo.wProcessorArchitecture)
                    {
                        case PROCESSOR_ARCHITECTURE_AMD64:
                        case PROCESSOR_ARCHITECTURE_IA64:
                            return "x64";

                        case PROCESSOR_ARCHITECTURE_ARM:
                            return "ARM";

                        case PROCESSOR_ARCHITECTURE_INTEL:
                            return "x86";

                        default:
                            return "Unknown";
                    }
                }
                catch (Exception ex)
                {
                    string noPiiMsg = CoreExceptionFactory.Instance.GetPiiScrubbedDetails(ex);
                    CoreLoggerBase.Default.Warning(noPiiMsg);
                    CoreLoggerBase.Default.WarningPii(ex.Message);
                    return "Unknown";
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct SYSTEM_INFO
            {
                public readonly short wProcessorArchitecture;
                public readonly short wReserved;
                public readonly int dwPageSize;
                public readonly IntPtr lpMinimumApplicationAddress;
                public readonly IntPtr lpMaximumApplicationAddress;
                public readonly IntPtr dwActiveProcessorMask;
                public readonly int dwNumberOfProcessors;
                public readonly int dwProcessorType;
                public readonly int dwAllocationGranularity;
                public readonly short wProcessorLevel;
                public readonly short wProcessorRevision;
            }
        }
    }
}