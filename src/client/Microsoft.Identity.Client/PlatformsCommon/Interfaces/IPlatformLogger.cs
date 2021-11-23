// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    internal interface IPlatformLogger
    {
        void Always(string message);
        void Error(string message);
        void Warning(string message);
        void Verbose(string message);
        void Information(string message);
    }
}
