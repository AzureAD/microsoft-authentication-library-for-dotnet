// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    internal interface IPlatformProxyFactory
    {
        public IPlatformProxy CreatePlatformProxy(ILoggerAdapter logger);
    }
}
