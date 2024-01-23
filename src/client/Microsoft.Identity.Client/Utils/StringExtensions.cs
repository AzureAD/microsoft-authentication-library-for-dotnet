// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

namespace Microsoft.Identity.Client.Utils
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Create an array of bytes representing the UTF-8 encoding of the given string.
        /// </summary>
        /// <param name="stringInput">String to get UTF-8 bytes for</param>
        /// <returns>Array of UTF-8 character bytes</returns>
        public static byte[] ToByteArray(this string stringInput)
        {
            return new UTF8Encoding().GetBytes(stringInput);
        }

        public static string NullIfEmpty(this string s)
        {
            return string.IsNullOrEmpty(s) ? null : s;
        }
        public static string NullIfWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
       

        /// <summary>
        /// Culture aware String.Replace
        /// </summary>
        public static string Replace(this string src, string oldValue, string newValue, StringComparison comparison)
        {
            if (string.IsNullOrWhiteSpace(src))
            {
                return src;
            }

            if (string.IsNullOrWhiteSpace(oldValue))
            { 
                throw new ArgumentException("oldValue cannot be empty"); 
            }

            // skip the loop entirely if oldValue and newValue are the same
            if (string.Compare(oldValue, newValue, comparison) == 0)
            { 
                return src; 
            }

            if (oldValue.Length > src.Length)
            { 
                return src;
            }

            var sb = new StringBuilder();

            int previousIndex = 0;
            int index = src.IndexOf(oldValue, comparison);

            while (index != -1)
            {
                sb.Append(src.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = src.IndexOf(oldValue, index, comparison);
            }

            sb.Append(src.Substring(previousIndex));

            return sb.ToString();
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

