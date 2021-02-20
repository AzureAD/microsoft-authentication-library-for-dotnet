// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Json.Linq;

using System;
using System.Text;

namespace Microsoft.Identity.Client.Kerberos
{
    /// <summary>
    /// Utility class to parse JWT token string.
    /// </summary>
    internal class KerberosIdTokenParser
    {
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
        /// Creates a <see cref="KerberosIdTokenParser"/> object from given JWT token string.
        /// </summary>
        /// <param name="raw">JWT token string to be parsed.</param>
        /// <returns>A <see cref="KerberosIdTokenParser"/> object containing parsed JWT token.</returns>
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

            byte[] payloadBytes = Convert.FromBase64String(sections[1]);
            string payload = Encoding.UTF8.GetString(payloadBytes);
            if (string.IsNullOrEmpty(payload))
            {
                return null;
            }

            KerberosIdTokenParser jwt = new KerberosIdTokenParser();
            jwt.PayloadJson = JObject.Parse(payload);
            return jwt;
        }
    }
}
