// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserDetailsClient.Core.Features.LogOn;

namespace MauiB2C.Platforms.Android
{
    class AndroidParentWindowLocatorService : IParentWindowLocatorService
    {
        internal object ParentWindow { get; set; }
        public object GetCurrentParentWindow() => ParentWindow;
    }
}
