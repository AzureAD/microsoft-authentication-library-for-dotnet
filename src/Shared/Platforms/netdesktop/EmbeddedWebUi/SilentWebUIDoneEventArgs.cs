// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if DESKTOP && MSAL_DESKTOP

using System;

namespace Microsoft.Identity.Client.Platforms.net45
{
    internal class SilentWebUIDoneEventArgs : EventArgs
    {
        public SilentWebUIDoneEventArgs(Exception e)
        {
            TransferedException = e;
        }

        public Exception TransferedException { get; private set; }
    }
}
#endif
