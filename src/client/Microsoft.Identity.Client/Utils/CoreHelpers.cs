// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Utils
{

    internal static class CoreHelpers
    {
        internal static string ByteArrayToString(byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(input, 0, input.Length);
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

            var queryPairs = SplitWithQuotes(input, delimiter);

            foreach (string queryPair in queryPairs)
            {
                var pair = SplitWithQuotes(queryPair, '=');

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
                        requestContext?.Logger.Warning(string.Format(CultureInfo.InvariantCulture,
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

        internal static IReadOnlyList<string> SplitWithQuotes(string input, char delimiter)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }

            var items = new List<string>();

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

        internal static string GetCcsClientInfoHint(string userObjectId, string userTenantID)
        {
            return (string.IsNullOrEmpty(userObjectId) || string.IsNullOrEmpty(userTenantID)) ? string.Empty : $@"oid:{userObjectId}@{userTenantID}";
        }

        internal static string GetCcsUpnHint(string upn)
        {
            return string.IsNullOrEmpty(upn)? string.Empty : $@"upn:{upn}";
        }
    }
}
