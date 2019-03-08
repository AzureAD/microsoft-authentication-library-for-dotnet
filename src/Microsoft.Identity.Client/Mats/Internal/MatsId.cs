// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal static class MatsId
    {
        public static string Create()
        {
            return Guid.NewGuid().AsMatsCorrelationId();
        }

        public static string AsMatsCorrelationId(this Guid correlationIdGuid)
        {
            #pragma warning disable CA1305 // Specify IFormatProvider
            return correlationIdGuid.ToString("D");
            #pragma warning restore CA1305 // Specify IFormatProvider
        }
    }
}
