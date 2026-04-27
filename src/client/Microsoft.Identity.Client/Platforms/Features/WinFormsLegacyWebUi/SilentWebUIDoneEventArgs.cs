// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    internal class SilentWebUIDoneEventArgs(Exception e) : EventArgs
    {
        public Exception TransferredException { get; private set; } = e;
    }
}
