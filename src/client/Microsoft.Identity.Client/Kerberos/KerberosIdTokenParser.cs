// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Json.Linq;

using System;
using System.Globalization;
using System.Text;

namespace Microsoft.Identity.Client.Kerberos
{
    /// <summary>
    /// Utility class to parse raw JWT token string and read each payload fields.
    /// </summary>
    internal class KerberosIdTokenParser
    {
        private const char Base64PadCharacter = '=';
        private const char Base64Character62 = '+';
        private const char Base64Character63 = '/';
        private const char Base64UrlCharacter62 = '-';
        private const char Base64UrlCharacter63 = '_';
        private static readonly string DoubleBase64PadCharacter = String.Format(CultureInfo.InvariantCulture, "{0}{0}", Base64PadCharacter);

        /// <summary>
        /// Parsed JSON object for JWT payload.
        /// </summary>
        private JObject PayloadJson;

        /// <summary>
        /// Gets a value associated with given key.
        /// </summary>
        /// <param name="key">Key value to be searched.</param>
        /// <returns>Value associated with given key value if key exists. Empty string, otherwise.</returns>
        internal string GetValueOrEmptyString(string key)
        {
            JToken value;
            if (this.PayloadJson.TryGetValue(key, out value))
            {
                return value.Value<string>();
            }

            return string.Empty;
        }

        /// <summary>
        /// Creates a <see cref="KerberosIdTokenParser"/> object from given raw JWT token string.
        /// </summary>
        /// <param name="raw">Raw JWT token string to be parsed.</param>
        /// <returns>A <see cref="KerberosIdTokenParser"/> object containing parsed JWT token payload.</returns>
        internal static KerberosIdTokenParser Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }

            string[] sections = raw.Split('.');
            if (sections.Length != 3)
            {
                return null;
            }

            byte[] payloadBytes = DecodeBytes(sections[1]);
            string payload = Encoding.UTF8.GetString(payloadBytes);
            if (string.IsNullOrEmpty(payload))
            {
                return null;
            }

            KerberosIdTokenParser jwt = new KerberosIdTokenParser();
            jwt.PayloadJson = JObject.Parse(payload);
            return jwt;
        }


        /// <summary>
        /// RAW token string from Azure AD is base64 url encoded.
        /// Decodes the given base64 url encoded raw token string.
        /// </summary>
        /// <param name="arg">Base64 url encoded token string.</param>
        /// <returns>Returns decoded 8-bit unsigned byte array.</returns>
        private static byte[] DecodeBytes(string arg)
        {
            string s = arg;
            s = s.Replace(Base64UrlCharacter62, Base64Character62); // 62nd char of encoding
            s = s.Replace(Base64UrlCharacter63, Base64Character63); // 63rd char of encoding
            switch (s.Length % 4)
            {
                case 0:
                    break; // No pad chars in this case
                case 2:
                    s += DoubleBase64PadCharacter;
                    break; // Two pad chars
                case 3:
                    s += Base64PadCharacter;
                    break; // One pad char
                default:
                    throw new ArgumentException("Illegal base64url string!", arg);
            }

            return Convert.FromBase64String(s); // Standard base64 decoder
        }
    }
}
