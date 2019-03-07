// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal static class MatsId
    {
        public static string Create()
        {
            #pragma warning disable CA1305 // Specify IFormatProvider
            return Guid.NewGuid().ToString("D");
            #pragma warning restore CA1305 // Specify IFormatProvider
        }
    }
}
