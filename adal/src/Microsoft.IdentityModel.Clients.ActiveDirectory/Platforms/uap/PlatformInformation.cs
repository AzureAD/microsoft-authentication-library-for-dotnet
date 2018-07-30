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

using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.System.UserProfile;
using Microsoft.Identity.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Windows.System;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform
{
    internal class PlatformInformation : PlatformInformationBase
    {
        public override string GetProductName()
        {
            return "PCL.UAP";
        }

        public override string GetEnvironmentVariable(string variable)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return localSettings.Values.ContainsKey(variable) ? localSettings.Values[variable].ToString() : null;
        }

        /// <summary>
        /// Get a principal name that can be used for WIA
        /// </summary>
        /// <remarks>
        /// Win10 allows several identities to be logged in at once; 
        /// select the first principal name that can be used
        /// </remarks>
        /// <returns></returns>
        public override async Task<string> GetUserPrincipalNameAsync()
        {
            IReadOnlyList<User> users = await User.FindAllAsync();
            if (users == null || !users.Any())
            {
                throw new AdalException(
                 AdalErrorEx.CannotAccessUserInformationOrUserNotDomainJoined,
                 CoreErrorMessages.UapCannotFindDomainUser);
            }

            var getUserDetailTasks = users.Select(async u =>
            {
                object domainObj = await u.GetPropertyAsync(KnownUserProperties.DomainName);
                string domainString = domainObj?.ToString();

                object principalObject = await u.GetPropertyAsync(KnownUserProperties.PrincipalName);
                string principalNameString = principalObject?.ToString();

                return new { Domain = domainString, PrincipalName = principalNameString };
            }).ToList();

            var userDetails = await Task.WhenAll(getUserDetailTasks).ConfigureAwait(false);

            // try to get a user that has both domain name and upn
            var userDetailWithDomainAndPn = userDetails.FirstOrDefault(
                d => !String.IsNullOrWhiteSpace(d.Domain) &&
                !String.IsNullOrWhiteSpace(d.PrincipalName));

            if (userDetailWithDomainAndPn != null)
            {
                return userDetailWithDomainAndPn.PrincipalName;
            }

            // try to get a user that at least has upn
            var userDetailWithPn = userDetails.FirstOrDefault(
              d => !String.IsNullOrWhiteSpace(d.PrincipalName));

            if (userDetailWithPn != null)
            {
                return userDetailWithPn.PrincipalName;
            }

            // user has domain name, but no upn -> missing Enterprise Auth capability
            if (userDetails.Any(d => !String.IsNullOrWhiteSpace(d.Domain)))
            {
                throw new AdalException(
                 AdalErrorEx.CannotAccessUserInformationOrUserNotDomainJoined,
                 CoreErrorMessages.UapCannotFindUpn);
            }

            // no domain, no upn -> missing User Info capability
            throw new AdalException(
             AdalErrorEx.CannotAccessUserInformationOrUserNotDomainJoined,
             CoreErrorMessages.UapCannotFindDomainUser);

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
            IReadOnlyList<User> users = await User.FindAllAsync();
            return users.Any(u => u.Type == UserType.LocalGuest || u.Type == UserType.LocalGuest);
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

            PromptBehavior promptBehavior = authorizationParameters.PromptBehavior;

            // ADFS currently ignores the parameter for now.
            switch (promptBehavior)
            {
                case PromptBehavior.Always:
                    authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.Login;
                    break;
                case PromptBehavior.SelectAccount:
                    authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.SelectAccount;
                    break;
                case PromptBehavior.RefreshSession:
                    authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.RefreshSession;
                    break;
                case PromptBehavior.Never:
                    authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.AttemptNone;
                    break;
            }
        }

        public override bool GetCacheLoadPolicy(IPlatformParameters parameters)
        {
            PlatformParameters authorizationParameters = (parameters as PlatformParameters);
            if (authorizationParameters == null)
            {
                throw new ArgumentException("parameters should be of type PlatformParameters", "parameters");
            }

            PromptBehavior promptBehavior = authorizationParameters.PromptBehavior;

            return promptBehavior != PromptBehavior.Always && promptBehavior != PromptBehavior.RefreshSession &&
                   promptBehavior != PromptBehavior.SelectAccount;
        }

        public override Uri ValidateRedirectUri(Uri redirectUri, RequestContext requestContext)
        {
            if (redirectUri == null)
            {
                redirectUri = Constant.SsoPlaceHolderUri;

                var msg = "ms-app redirect Uri is used";
                requestContext.Logger.Verbose(msg);
                requestContext.Logger.VerbosePii(msg);
            }

            return redirectUri;
        }

        public override string GetRedirectUriAsString(Uri redirectUri, RequestContext requestContext)
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
