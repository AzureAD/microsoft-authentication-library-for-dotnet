// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if WINDOWS_APP
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal interface IAccountPicker
    {
        Task<WebAccountProvider> DetermineAccountInteractivelyAsync();
    }
}
#endif
