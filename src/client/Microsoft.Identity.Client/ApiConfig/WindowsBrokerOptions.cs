// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client
{

    /// <summary>
    /// Advanced options for using the Windows 10 broker.
    /// For more details see https://aka.ms/msal-net-wam
    /// </summary>
#if !SUPPORTS_BROKER || __MOBILE__
    [EditorBrowsable(EditorBrowsableState.Never)]
#endif    
    public class WindowsBrokerOptions
    {
        /// <summary>
        /// 
        /// </summary>
        public WindowsBrokerOptions()
        {
            ValidatePlatformAvailability();
        }

        internal static WindowsBrokerOptions CreateDefault()
        {
            return new WindowsBrokerOptions();
        }

        /// <summary>
        /// A legacy option available only to Microsoft applications. Should be avoided where possible.
        /// Support is experimental.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)] // 1p feature only, hide it from public API.
        public bool MsaPassthrough { get; set; } = false;

        /// <summary>
        /// Allow the Windows broker to list Work and School accounts as part of the <see cref="ClientApplicationBase.GetAccountsAsync()"/>
        /// </summary>
        /// <remarks>On UWP, accounts are not listed due to privacy concerns</remarks>
        public bool ListWindowsWorkAndSchoolAccounts { get; set; } = false;

        /// <summary>
        /// Display a custom text in the broker UI controls which support it. 
        /// </summary>
        /// <remarks>
        /// Currently only the WAM account picker allows for this customization, see <see href="https://docs.microsoft.com/en-us/windows/uwp/security/web-account-manager#add-a-custom-header">WAM documentation</see>.
        /// </remarks>
        public string HeaderText { get; set; }

        internal static void ValidatePlatformAvailability()
        {
#if __MOBILE__
            throw new MsalClientException("not_supported", "These options only affect the Windows 10 Broker");
#endif
        }
    }
}
