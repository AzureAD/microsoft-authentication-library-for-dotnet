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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Identity.Core;

namespace Microsoft.Identity.Client.Internal
{
    internal static class MsalHelpers
    {
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
            if (IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return string.Join(" ", input);
        }

        internal static SortedSet<string> AsSet(this string singleString)
        {
            if (string.IsNullOrEmpty(singleString))
            {
                return new SortedSet<string>();
            }

            return new SortedSet<string>(singleString.Split(new[] { " " }, StringSplitOptions.None));
        }

        internal static string[] AsArray(this string singleString)
        {
            if (string.IsNullOrWhiteSpace(singleString))
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
        
        internal static bool IsNullOrEmpty(IEnumerable<string> input)
        {
            return input == null || !input.Any();
        }

        internal static string ByteArrayToString(byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(input, 0, input.Length);
        }

        public static DateTime UnixTimestampToDateTime(double unixTimestamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimestamp).ToUniversalTime();
            return dateTime;
        }

        public static long DateTimeToUnixTimestamp(DateTimeOffset dateTimeOffset)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            long unixTimestamp = (long)dateTimeOffset.Subtract(dateTime).TotalSeconds;
            return unixTimestamp;
        }

        public static long DateTimeToUnixTimestampMilliseconds(DateTimeOffset dateTimeOffset)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            long unixTimestamp = (long)dateTimeOffset.Subtract(dateTime).TotalMilliseconds;
            return unixTimestamp;
        }

        public static string CreateString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static string UrlEncode(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            message = Uri.EscapeDataString(message);
            message = message.Replace("%20", "+");

            return message;
        }

        public static string UrlDecode(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            message = message.Replace("+", "%20");
            message = Uri.UnescapeDataString(message);

            return message;
        }

        public static void AddKeyValueString(StringBuilder messageBuilder, string key, string value)
        {
            AddKeyValueString(messageBuilder, key, value.ToCharArray());
        }

        public static string ToQueryParameter(this IDictionary<string, string> input)
        {
            StringBuilder builder = new StringBuilder();

            if (input.Count > 0)
            {
                foreach (var key in input.Keys)
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", key, UrlEncode(input[key]));
                }

                if (builder.Length > 0)
                {
                    builder.Remove(builder.Length - 1, 1);
                }
            }

            return builder.ToString();
        }

        public static Dictionary<string, string> ParseKeyValueList(string input, char delimiter, bool urlDecode,
            bool lowercaseKeys,
            RequestContext requestContext)
        {
            var response = new Dictionary<string, string>();

            List<string> queryPairs = SplitWithQuotes(input, delimiter);

            foreach (string queryPair in queryPairs)
            {
                List<string> pair = SplitWithQuotes(queryPair, '=');

                if (pair.Count == 2 && !string.IsNullOrWhiteSpace(pair[0]) && !string.IsNullOrWhiteSpace(pair[1]))
                {
                    string key = pair[0];
                    string value = pair[1];

                    // Url decoding is needed for parsing OAuth response, but not for parsing WWW-Authenticate header in 401 challenge
                    if (urlDecode)
                    {
                        key = UrlDecode(key);
                        value = UrlDecode(value);
                    }

                    if (lowercaseKeys)
                    {
                        key = key.Trim().ToLowerInvariant();
                    }

                    value = value.Trim().Trim('\"').Trim();

                    if (response.ContainsKey(key))
                    {
                        var msg = string.Format(CultureInfo.InvariantCulture,
                            "Key/value pair list contains redundant key '{0}'.", key);
                        requestContext?.Logger.Warning(msg);
                        requestContext?.Logger.WarningPii(msg);
                    }

                    response[key] = value;
                }
            }

            return response;
        }

        public static Dictionary<string, string> ParseKeyValueList(string input, char delimiter, bool urlDecode,
            RequestContext requestContext)
        {
            return ParseKeyValueList(input, delimiter, urlDecode, true, requestContext);
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

        internal static List<string> SplitWithQuotes(string input, char delimiter)
        {
            List<string> items = new List<string>();

            if (string.IsNullOrWhiteSpace(input))
            {
                return items;
            }

            int startIndex = 0;
            bool insideString = false;
            string item;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == delimiter && !insideString)
                {
                    item = input.Substring(startIndex, i - startIndex);
                    if (!string.IsNullOrWhiteSpace(item.Trim()))
                    {
                        items.Add(item);
                    }

                    startIndex = i + 1;
                }
                else if (input[i] == '"')
                {
                    insideString = !insideString;
                }
            }

            item = input.Substring(startIndex);
            if (!string.IsNullOrWhiteSpace(item.Trim()))
            {
                items.Add(item);
            }

            return items;
        }

        public static void AppendQueryParameters(this UriBuilder builder, string queryParams)
        {
            if (builder == null || string.IsNullOrEmpty(queryParams))
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

        private static void AddKeyValueString(StringBuilder messageBuilder, string key, char[] value)
        {
            string delimiter = (messageBuilder.Length == 0) ? string.Empty : "&";
            messageBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}=", delimiter, key);
            messageBuilder.Append(value);
        }
    }
}