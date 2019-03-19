// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal static class StringExtensions
    {
        public static string TrimCurlyBraces(this string input)
        {
#pragma warning disable CA1305 // Specify IFormatProvider
            return Guid.Parse(input).ToString("D");
#pragma warning restore CA1305 // Specify IFormatProvider
        }
    }
}
