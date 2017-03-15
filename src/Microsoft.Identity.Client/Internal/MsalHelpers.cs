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

namespace Microsoft.Identity.Client.Internal
{
    internal static class MsalHelpers
    {
        public static bool ScopeContains(this SortedSet<string> scope, SortedSet<string> otherScope)
        {
            foreach (string otherString in otherScope)
            {
                if (!scope.Contains(otherString))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ScopeIntersects(this SortedSet<string> scope, SortedSet<string> otherScope)
        {
            return scope.Overlaps(otherScope);
        }

        internal static string[] AsArray(this SortedSet<string> setOfStrings)
        {
            if (setOfStrings == null)
            {
                return null;
            }

            return setOfStrings.ToArray();
        }

        internal static string AsSingleString(this SortedSet<string> setOfStrings)
        {
            return AsSingleString(setOfStrings.ToArray());
        }

        internal static string AsSingleString(this string[] arrayOfStrings)
        {
            if (IsNullOrEmpty(arrayOfStrings))
            {
                return String.Empty;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(arrayOfStrings[0]);

            for (int i = 1; i < arrayOfStrings.Length; i++)
            {
                sb.Append(" ");
                sb.Append(arrayOfStrings[i]);
            }

            return sb.ToString();
        }

        internal static SortedSet<string> AsSet(this string singleString)
        {
            if (String.IsNullOrEmpty(singleString))
            {
                return new SortedSet<string>();
            }

            return new SortedSet<string>(singleString.Split(new[] {" "}, StringSplitOptions.None));
        }

        internal static string[] AsArray(this string singleString)
        {
            if (String.IsNullOrWhiteSpace(singleString))
            {
                return new string[] {};
            }

            return singleString.Split(new[] {" "}, StringSplitOptions.None);
        }

        internal static SortedSet<string> CreateSetFromArray(this string[] arrayStrings)
        {
            SortedSet<string> set = new SortedSet<string>();
            if (arrayStrings == null || arrayStrings.Length == 0)
            {
                return set;
            }

            foreach (string str in arrayStrings)
            {
                set.Add(str);
            }

            return set;
        }
        
        internal static bool IsNullOrEmpty(string[] input)
        {
            return input == null || input.Length == 0;
        }

        internal static string ByteArrayToString(byte[] input)
        {
            return Encoding.UTF8.GetString(input, 0, input.Length);
        }

        public static DateTime UnixTimestampToDateTime(double unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }

        public static long DateTimeToUnixTimestamp(DateTimeOffset dtOffset)
        {
            return (long)(dtOffset.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc))).TotalSeconds;
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

            foreach (var key in input.Keys)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", key, UrlEncode(input[key]));
            }

            if (builder.Length > 0)
            {
                builder.Remove(builder.Length - 1, 1);
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

                    value = value.Trim().Trim(new[] { '\"' }).Trim();

                    if (response.ContainsKey(key) && requestContext != null)
                    {
                        requestContext.Logger.Warning(string.Format(CultureInfo.InvariantCulture,
                                "Key/value pair list contains redundant key '{0}'.", key));
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

        public static string EncodeToBase64Url(string input)
        {
            return EncodeToBase64Url(ToByteArray(input));
        }

        public static string EncodeToBase64Url(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static string DecodeFromBase64Url(string returnValue)
        {
            string incoming = returnValue
                .Replace('_', '/').Replace('-', '+');
            switch (returnValue.Length % 4)
            {
                case 2:
                    incoming += "==";
                    break;
                case 3:
                    incoming += "=";
                    break;
            }
            byte[] bytes = Convert.FromBase64String(incoming);
            return CreateString(bytes);
        }

        internal static string Base64Decode(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            byte[] bytes = Convert.FromBase64String(input);
            return CreateString(bytes);
        }

        internal static string Base64Encode(string input)
        {
            string encodedString = String.Empty;
            if (!String.IsNullOrEmpty(input))
            {
                encodedString = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
            }

            return encodedString;
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

        private static void AddKeyValueString(StringBuilder messageBuilder, string key, char[] value)
        {
            string delimiter = (messageBuilder.Length == 0) ? string.Empty : "&";
            messageBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}=", delimiter, key);
            messageBuilder.Append(value);
        }
    }
}