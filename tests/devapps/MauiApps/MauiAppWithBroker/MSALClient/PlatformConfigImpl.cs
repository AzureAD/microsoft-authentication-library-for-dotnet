// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiAppWithBroker.MSALClient
{
    /// <summary>
    /// Platform specific configuration.
    /// </summary>
    public class PlatformConfigImpl
    {
        /// <summary>
        /// Instance to store data
        /// </summary>
        public static PlatformConfigImpl Instance { get; } = new PlatformConfigImpl();

        /// <summary>
        /// Platform specific Redirect URI
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// Platform specific parent window
        /// </summary>
        public object ParentWindow { get; set; }

        // private constructor to ensure singleton
        private PlatformConfigImpl()
        {
        }
    }
}
