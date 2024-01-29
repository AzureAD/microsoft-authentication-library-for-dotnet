// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
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
using Microsoft.Win32;
using Microsoft.Identity.Client.Platforms.netdesktop;
using Microsoft.Identity.Client.Platforms.netcore;

namespace Microsoft.Identity.Client.Platforms.netdesktop472
{
    /// <summary>
    ///     Platform / OS specific logic.
    /// </summary>
    internal class NetDesktop472PlatformProxy : NetDesktopPlatformProxy
    {
        /// <inheritdoc/>
        public NetDesktop472PlatformProxy(ILoggerAdapter logger)
            : base(logger)
        {
        }

        public override IKeyMaterialManager GetKeyMaterialManager()
        {
            return new ManagedIdentityCertificateProvider(Logger);
        }
    }
}
