// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

namespace Microsoft.Identity.Client.Utils
{
    internal static class StringExtensions
    {
        static UTF8Encoding utf8Encoding = new();

        /// <summary>
        /// Create an array of bytes representing the UTF-8 encoding of the given string.
        /// </summary>
        /// <param name="stringInput">String to get UTF-8 bytes for</param>
        /// <returns>Array of UTF-8 character bytes</returns>
        public static byte[] ToByteArray(this string stringInput)
        {
            return utf8Encoding.GetBytes(stringInput);
        }

        public static string NullIfEmpty(this string s)
        {
            return string.IsNullOrEmpty(s) ? null : s;
        }
        public static string NullIfWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

#if NETSTANDARD2_0 || NETFRAMEWORK || WINDOWS_APP
        /// <summary>
        /// Culture aware Contains
        /// </summary>
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source != null && toCheck != null && source.IndexOf(toCheck, comp) >= 0;
        }
#endif
    }
}

