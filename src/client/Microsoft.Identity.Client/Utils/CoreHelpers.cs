// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

            return message;
        }

        public static string UrlDecode(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }
           // Replace "+" with "%20" for backward compatibility with older systems that used "+" for spaces.
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

        public static Dictionary<string, string> ParseKeyValueList(
            string input,
            char delimiter,
            bool urlDecode,
            bool lowercaseKeys,
            RequestContext requestContext)
        {
            var response = new Dictionary<string, string>();

            // Split the full query string on & (or any provided delimiter) to get individual k=v pairs.
            var queryPairs = SplitWithQuotes(input, delimiter);

            foreach (string queryPair in queryPairs)
            {
                // Instead of splitting on *all* '=' characters, find only the first one.
                // This ensures that if the value itself contains '=', such as a trailing '=' in Base64,
                // we do not accidentally split the base64 value into extra parts and lose the padding.
                int idx = queryPair.IndexOf('=');

                // idx > 0 means we found an '=' and have a valid key substring before it
                if (idx > 0)
                {
                    // The key is everything before the first '='
                    string key = queryPair.Substring(0, idx);

                    // The value is everything after the first '=' (including any trailing '=')
                    string value = queryPair.Substring(idx + 1);

                    // Url decoding is needed for parsing OAuth response, but not for parsing WWW-Authenticate header in 401 challenge
                    if (urlDecode)
                    {
                        key = UrlDecode(key);
                        value = UrlDecode(value);
                    }

                    // Optionally convert key to lowercase
                    if (lowercaseKeys)
                    {
                        key = key.Trim().ToLowerInvariant();
                    }

                    // Trim quotes and whitespace around the value
                    value = value.Trim().Trim('\"').Trim();

                    if (response.ContainsKey(key))
                    {
                        requestContext?.Logger.Warning(
                            string.Format(CultureInfo.InvariantCulture,
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

        // Helper method intended to help deprecate some WithExtraQueryParameters APIs.
        // Convert from Dictionary<string, string> to Dictionary<string, (string value, bool includeInCacheKey)>,
        // with all includeInCacheKey set to false by default to maintain existing behavior of those older APIs.
        internal static IDictionary<string, (string value, bool includeInCacheKey)> ConvertToTupleParameters(IDictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                return null;
            }

            var result = new Dictionary<string, (string value, bool includeInCacheKey)>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in parameters)
            {
                result[kvp.Key] = (kvp.Value, false); // Exclude all parameters from cache key by default
            }
            return result;
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

        internal static string ComputeAccessTokenExtCacheKey(SortedList<string, string> cacheKeyComponents)
        {
            if (cacheKeyComponents == null || !cacheKeyComponents.Any())
            {
                return string.Empty;
            }

            StringBuilder stringBuilder = new();

            foreach (var component in cacheKeyComponents)
            {
                stringBuilder.Append(component.Key);
                stringBuilder.Append(component.Value);
            }

            using (SHA256 hash = SHA256.Create())
            {
                var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
                return Base64UrlHelpers.Encode(hashBytes);
            }
        }

        internal static string ComputeX5tS256KeyId(X509Certificate2 certificate)
        {
            // Extract the raw bytes of the certificate’s public key.
            var publicKey = certificate.GetPublicKey();

            // Compute the SHA-256 hash of the public key.
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(publicKey);

                // Return the hash encoded in Base64 URL format.
                return Base64UrlHelpers.Encode(hash);
            }
        }
    }
}
