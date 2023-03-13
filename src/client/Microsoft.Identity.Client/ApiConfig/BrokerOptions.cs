// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace Microsoft.Identity.Client.ApiConfig
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
        /// <param name="operatingSystems">Choices of OperatingSystems</param>
        public BrokerOptions(OperatingSystems operatingSystems)
        {
            if (operatingSystems == OperatingSystems.None)
            {
                throw new ArgumentException($"Operating system must be specified.");
            }
        }

        // The default constructor is private. So developer is forced to set the OS choice(s)
        private BrokerOptions()
        {

        }

        /// <summary>
        /// Creates default options that can be modified later except the choice of OS
        /// </summary>
        /// <param name="osChoice">Choice of OS platforms</param>
        /// <param name="listWorkAndSchoolAccts">List wokr and school accounts</param>
        /// <returns></returns>
        internal static BrokerOptions CreateDefault()
        {
            return new BrokerOptions()
            {
                OSChoices = OperatingSystems.Windows,
                 ListOperatingSystemAccounts = true,
            };
        }

        /// <summary>
        /// Creates BrokerOptions from WindowsBrokerOptions
        /// </summary>
        /// <param name="winOptions"></param>
        /// <param name="osChoice"></param>
        /// <returns></returns>
        internal static BrokerOptions CreateFromWindowsOptions(WindowsBrokerOptions winOptions, OperatingSystems osChoice = OperatingSystems.Windows)
        {
            BrokerOptions ret = new BrokerOptions(osChoice);
            ret.Title = winOptions.HeaderText;
            ret.MsaPassthrough = winOptions.MsaPassthrough;
            ret.ListOperatingSystemAccounts = winOptions.ListWindowsWorkAndSchoolAccounts;

            return ret;
        }

        /// <summary>
        /// This is a required property to determine the supported OS
        /// </summary>
        public OperatingSystems OSChoices { get; private set; }

        /// <summary>
        /// Title of the broker window
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// A legacy option available only to Microsoft applications. Should be avoided where possible.
        /// Support is experimental.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)] // 1p feature only, hide it from public API.
        public bool MsaPassthrough { get; set; } = false;

        /// <summary>
        /// Currently only supported on Windows
        /// Allow the Windows broker to list Work and School accounts as part of the <see cref="ClientApplicationBase.GetAccountsAsync()"/>
        /// </summary>
        /// <remarks>On UWP, accounts are not listed due to privacy concerns</remarks>/// 
        public bool ListOperatingSystemAccounts { get; set; }
    }
}
