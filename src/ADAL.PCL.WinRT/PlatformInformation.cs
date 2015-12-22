//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.System.UserProfile;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class PlatformInformation : PlatformInformationBase
    {
        public override string GetProductName()
        {
            return "PCL.WinRT";
        }

        public override string GetEnvironmentVariable(string variable)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return localSettings.Values.ContainsKey(variable) ? localSettings.Values[variable].ToString() : null;
        }

        public override async Task<string> GetUserPrincipalNameAsync()
        {
            if (!UserInformation.NameAccessAllowed)
            {
                throw new AdalException(AdalErrorEx.CannotAccessUserInformation, AdalErrorMessageEx.CannotAccessUserInformation);
            }

            try
            {
                return await UserInformation.GetPrincipalNameAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new AdalException(AdalErrorEx.UnauthorizedUserInformationAccess, AdalErrorMessageEx.UnauthorizedUserInformationAccess, ex);
            }
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

        public override async Task<bool> IsUserLocalAsync(CallState callState)
        {
            if (!UserInformation.NameAccessAllowed)
            {
                // The access is not allowed and we cannot determine whether this is a local user or not. So, we do NOT add form auth parameter.
                // This is the case where we can advise customers to add extra query parameter if they want.

                PlatformPlugin.Logger.Information(callState, "Cannot access user information to determine whether it is a local user or not due to machine's privacy setting.");
                return false;
            }

            try
            {
                return string.IsNullOrEmpty(await UserInformation.GetDomainNameAsync());
            }
            catch (UnauthorizedAccessException)
            {
                PlatformPlugin.Logger.Information(callState, "Cannot try Windows Integrated Auth due to lack of Enterprise capability.");
                // This mostly means Enterprise capability is missing, so WIA cannot be used and
                // we return true to add form auth parameter in the caller.
                return true;
            }            
        }

        public override bool IsDomainJoined()
        {
            return NetworkInformation.GetHostNames().Any(entry => entry.Type == HostNameType.DomainName);
        }

        public override void AddPromptBehaviorQueryParameter(IPlatformParameters parameters, DictionaryRequestParameters authorizationRequestParameters)
        {
            PlatformParameters authorizationParameters = (parameters as PlatformParameters);
            if (authorizationParameters == null)
            {
                throw new ArgumentException("parameters should be of type PlatformParameters", "parameters");
            }

            PromptBehavior promptBehavior = (parameters as PlatformParameters).PromptBehavior;

            // ADFS currently ignores the parameter for now.
            switch (promptBehavior)
            {
                case PromptBehavior.Always:
                    authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.Login;
                    break;
                case PromptBehavior.RefreshSession:
                    authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.RefreshSession;
                    break;
                case PromptBehavior.Never:
                    authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.AttemptNone;
                    break;
            }
        }

        public override Uri ValidateRedirectUri(Uri redirectUri, CallState callState)
        {
            if (redirectUri == null)
            {
                redirectUri = Constant.SsoPlaceHolderUri;
                PlatformPlugin.Logger.Verbose(callState, "ms-app redirect Uri is used");
            }

            return redirectUri;
        }

        public override string GetRedirectUriAsString(Uri redirectUri, CallState callState)
        {
            return ReferenceEquals(redirectUri, Constant.SsoPlaceHolderUri) ?
                WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString :
                redirectUri.OriginalString;
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
                catch
                {
                    return "Unknown";
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct SYSTEM_INFO
            {
                public short wProcessorArchitecture;
                public short wReserved;
                public int dwPageSize;
                public IntPtr lpMinimumApplicationAddress;
                public IntPtr lpMaximumApplicationAddress;
                public IntPtr dwActiveProcessorMask;
                public int dwNumberOfProcessors;
                public int dwProcessorType;
                public int dwAllocationGranularity;
                public short wProcessorLevel;
                public short wProcessorRevision;
            }
        }
    }
}
