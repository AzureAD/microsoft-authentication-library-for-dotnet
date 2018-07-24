//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using Microsoft.Identity.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.Identity.Core.Helpers
{
    internal static class ExtensionMethods
    {
        public static void AppendQueryParameters(this UriBuilder builder, string queryParams)
        {
            if (builder == null || String.IsNullOrEmpty(queryParams))
            {
                return;
            }

            if (builder.Query.Length > 1)
            {
                builder.Query = builder.Query.Substring(1) + "&" + queryParams;
            }
            else
            {
                builder.Query = queryParams;
            }
        }

        public static byte[] ToByteArray(this String stringInput)
        {
            return ToByteArray(new StringBuilder(stringInput));
        }

        public static byte[] ToByteArray(this StringBuilder stringBuilder)
        {
            if (stringBuilder == null)
            {
                return null;
            }

            UTF8Encoding encoding = new UTF8Encoding();
            var messageChars = new char[stringBuilder.Length];

            try
            {
                stringBuilder.CopyTo(0, messageChars, 0, stringBuilder.Length);
                return encoding.GetBytes(messageChars);
            }
            finally
            {
                messageChars.SecureClear();
            }
        }

        public static void SecureClear(this StringBuilder stringBuilder)
        {
            if (stringBuilder != null)
            {
                for (int i = 0; i < stringBuilder.Length; i++)
                {
                    stringBuilder[i] = '\0';
                }

                stringBuilder.Length = 0;
            }
        }

        public static void SecureClear(this byte[] bytes)
        {
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = 0;
                }
            }
        }

        public static void SecureClear(this char[] chars)
        {
            if (chars != null)
            {
                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = '\0';
                }
            }
        }

        public static string ToStringInvariant(this int val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToStringInvariant(this long val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }

        public static bool ScopeContains(this SortedSet<string> scopes, SortedSet<string> otherScope)
        {
            foreach (string otherString in otherScope)
            {
                if (!scopes.Contains(otherString, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ScopeIntersects(this SortedSet<string> scopes, SortedSet<string> otherScope)
        {
            return scopes.Overlaps(otherScope);
        }

        internal static string[] AsArray(this SortedSet<string> setOfStrings)
        {
            return setOfStrings?.ToArray();
        }

        internal static string AsSingleString(this IEnumerable<string> input)
        {
            if (CoreHelpers.IsNullOrEmpty(input))
            {
                return String.Empty;
            }

            return String.Join(" ", input);
        }

        internal static SortedSet<string> AsLowerCaseSortedSet(this string singleString)
        {
            if (String.IsNullOrEmpty(singleString))
            {
                return new SortedSet<string>();
            }

            return new SortedSet<string>(singleString.ToLowerInvariant().Split(new[] { " " }, StringSplitOptions.None));
        }

        internal static string[] AsArray(this string singleString)
        {
            if (String.IsNullOrWhiteSpace(singleString))
            {
                return new string[] { };
            }

            return singleString.Split(new[] { " " }, StringSplitOptions.None);
        }

        internal static SortedSet<string> CreateSetFromEnumerable(this IEnumerable<string> input)
        {
            if (input == null || !input.Any())
            {
                return new SortedSet<string>();
            }
            return new SortedSet<string>(input);
        }
    }
}
