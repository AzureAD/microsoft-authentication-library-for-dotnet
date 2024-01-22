// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;

namespace Microsoft.Identity.Client.Platforms.uap.WamBroker
{
    internal interface IAccountPickerFactory
    {
        IAccountPicker Create(
            IntPtr parentHandle,
            ILoggerAdapter logger,
            SynchronizationContext synchronizationContext,
            Authority authority,
            bool isMsaPassthrough, 
            string optionalHeaderText);
    }

    internal class AccountPickerFactory : IAccountPickerFactory
    {
        public IAccountPicker Create(
            IntPtr parentHandle, 
            ILoggerAdapter logger, 
            SynchronizationContext synchronizationContext, 
            Authority authority, 
            bool isMsaPassthrough, 
            string optionalHeaderText)
        {
            return new AccountPicker(
                 parentHandle,
                 logger,
                 synchronizationContext,
                 authority,
                 isMsaPassthrough,
                 optionalHeaderText);
        }
    }

}
