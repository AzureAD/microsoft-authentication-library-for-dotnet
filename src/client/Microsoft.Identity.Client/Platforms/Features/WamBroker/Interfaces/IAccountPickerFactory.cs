// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal interface IAccountPickerFactory
    {
        IAccountPicker Create(
            IntPtr parentHandle,
            ICoreLogger logger,
            SynchronizationContext synchronizationContext,
            Authority authority,
            bool isMsaPassthrough);
    }

    internal class AccountPickerFactory : IAccountPickerFactory
    {
        public IAccountPicker Create(IntPtr parentHandle, ICoreLogger logger, SynchronizationContext synchronizationContext, Authority authority, bool isMsaPassthrough)
        {
            return new AccountPicker(
                 parentHandle,
                 logger,
                 synchronizationContext,
                 authority,
                 isMsaPassthrough);
        }
    }

}
