// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    internal class SilentWebUIDoneEventArgs : EventArgs
    {
        public SilentWebUIDoneEventArgs(Exception e)
        {
            TransferredException = e;
        }

        public Exception TransferredException { get; private set; }
    }
}
