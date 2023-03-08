// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <summary>
    /// The class specifies the options for broker across OSs
    /// The common properties are direct members
    /// Platform specific properties (if they exist) are part of the corresponding options
    /// </summary>
    public class BrokerOptions
    {
        /// <summary>
        /// Supported OSs
        /// </summary>
        [Flags]
        public enum OSs
        {
            /// <summary>
            /// No OS specified - Invalid options
            /// </summary>
            None = 0b_0000_0000,  // 0
            /// <summary>
            /// Use broker on Windows OS
            /// </summary>
            Windows = 0b_0000_0001,  // 1
            /// <summary>
            /// Use broker on Mac OS
            /// </summary>
            MacOS = 0b_0000_0010,  // 2

            /// <summary>
            /// Use broker on Mac and Windows OS
            /// </summary>
            WindowsAndMac = Windows | MacOS,
        }

        /// <summary>
        /// Class specifying options for windows platform
        /// </summary>
        public class WindowsOptions
        {
            /// <summary>
            /// Allow the Windows broker to list Work and School accounts as part of the <see cref="ClientApplicationBase.GetAccountsAsync()"/>
            /// </summary>
            /// <remarks>On UWP, accounts are not listed due to privacy concerns</remarks>
            public bool ListWindowsWorkAndSchoolAccounts { get; set; } = false;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="osChoices">Choices of OSs</param>
        /// <param name="title">Title of the broker</param>
        /// <param name="msaPassthrough">is this MsaPasstorugh</param>
        public BrokerOptions(OSs osChoices, string title = "", bool msaPassthrough = false)
        {
            if (osChoices == OSs.None)
            {
                throw new ArgumentException($"osChoices should not be none");
            }

            OSChoices = osChoices;
            Title = title;
            MsaPassthrough = msaPassthrough;
        }

        // The default constructor is private. So developer is forced to set the OS choice(s)
        private BrokerOptions()
        {

        }

        /// <summary>
        /// Creates default options that can be modified except the choice of OS
        /// </summary>
        /// <param name="osChoice">Choice of OS platforms</param>
        /// <param name="listWorkAndSchoolAccts">List wokr and school accounts</param>
        /// <returns></returns>
        public static BrokerOptions CreateDefault(OSs osChoice = OSs.Windows, bool listWorkAndSchoolAccts = true)
        {
            BrokerOptions ret = new BrokerOptions(osChoice);
            var winBrokerDefaultOptions = WindowsBrokerOptions.CreateDefault();
            ret.Title = winBrokerDefaultOptions.HeaderText;
            ret.MsaPassthrough = winBrokerDefaultOptions.MsaPassthrough;
            ret.WindowsOSOptions = new WindowsOptions();
            ret.WindowsOSOptions.ListWindowsWorkAndSchoolAccounts = listWorkAndSchoolAccts;
            
            return ret;
        }

        /// <summary>
        /// Creates BrokerOptions from WindowsBrokerOptions
        /// </summary>
        /// <param name="winOptions"></param>
        /// <param name="osChoice"></param>
        /// <returns></returns>
        public static BrokerOptions CreateFromWindowsOptions(WindowsBrokerOptions winOptions, OSs osChoice = OSs.Windows)
        {
            BrokerOptions ret = new BrokerOptions(osChoice);
            ret.Title = winOptions.HeaderText;
            ret.MsaPassthrough = winOptions.MsaPassthrough;
            ret.WindowsOSOptions = new WindowsOptions();
            ret.WindowsOSOptions.ListWindowsWorkAndSchoolAccounts = winOptions.ListWindowsWorkAndSchoolAccounts;

            return ret;
        }

        /// <summary>
        /// This is a required property to determine the supported OS
        /// </summary>
        public OSs OSChoices { get; private set; }

        /// <summary>
        /// Title of the broker
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Options specific to windows platform
        /// </summary>
        public WindowsOptions WindowsOSOptions { get; set; }

        /// <summary>
        /// A legacy option available only to Microsoft applications. Should be avoided where possible.
        /// Support is experimental.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)] // 1p feature only, hide it from public API.
        public bool MsaPassthrough { get; set; } = false;

        /// <summary>
        /// This is to validate the options
        /// </summary>
        internal void Validate()
        { 
            if(OSChoices == OSs.None)
            {
                throw new InvalidOperationException($"");
            }
        }
    }
}
