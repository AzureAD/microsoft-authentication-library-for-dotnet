// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// The class specifies the options for broker across OperatingSystems
    /// The common properties are direct members
    /// Platform specific properties (if they exist) are part of the corresponding options
    /// </summary>
    public class BrokerOptions
    {
        /// <summary>
        /// Supported OperatingSystems
        /// </summary>
        [Flags]
        public enum OperatingSystems
        {
            /// <summary>
            /// No OS specified - Invalid options
            /// </summary>
            None = 0b_0000_0000,  // 0
            /// <summary>
            /// Use broker on Windows OS
            /// </summary>
            Windows = 0b_0000_0001,  // 1
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enabledOn">Choices of OperatingSystems</param>
        public BrokerOptions(OperatingSystems enabledOn)
        {
            EnabledOn = enabledOn;
        }

        /// <summary>
        /// Creates BrokerOptions from WindowsBrokerOptions
        /// </summary>
        internal static BrokerOptions CreateFromWindowsOptions(WindowsBrokerOptions winOptions)
        {
            BrokerOptions ret = new BrokerOptions(OperatingSystems.Windows);
            ret.Title = winOptions.HeaderText;
            ret.MsaPassthrough = winOptions.MsaPassthrough;
            ret.ListOperatingSystemAccounts = winOptions.ListWindowsWorkAndSchoolAccounts;

            return ret;
        }

        /// <summary>
        /// Operating systems on which broker is enabled.
        /// </summary>
        public OperatingSystems EnabledOn { get; }

        /// <summary>
        /// Title of the broker window
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// A legacy option available only to Microsoft First-Party applications. Should be avoided where possible.
        /// </summary>
        /// <remarks>This is a convenience API, the same can be achieved by using WithExtraQueryParameters and passing the extra query parameter "msal_request_type": "consumer_passthrough"</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)] // 1p feature only, hide it from public API.
        public bool MsaPassthrough { get; set; } = false;

        /// <summary>
        /// Currently only supported on Windows
        /// Allows the Windows broker to list Work and School accounts as part of the <see cref="ClientApplicationBase.GetAccountsAsync()"/>
        /// </summary>        
        public bool ListOperatingSystemAccounts { get; set; }

        internal bool IsBrokerEnabledOnCurrentOs()
        {
            if (EnabledOn.HasFlag(OperatingSystems.Windows) && DesktopOsHelper.IsWindows())
            {
                return true;
            }

            return false;
        }
    }
}
