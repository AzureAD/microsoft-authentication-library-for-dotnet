// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Nodes;
#else
using Newtonsoft.Json.Linq;
#endif

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// Utility for extracting claims from JWT tokens.
    /// </summary>
    internal static class JwtHelper
    {
        /// <summary>
        /// Extracts the "exp" (expiration) and "iat" (issued at) claims from a JWT token.
        /// </summary>
        /// <param name="jwt">The JWT token string (format: header.payload.signature).</param>
        /// <param name="issuedAt">The issued at time in UTC, or DateTimeOffset.MinValue if not found.</param>
        /// <param name="expiresAt">The expiration time in UTC, or DateTimeOffset.MinValue if not found.</param>
        /// <returns>True if both claims were successfully extracted; false otherwise.</returns>
        public static bool TryExtractTimestamps(string jwt, out DateTimeOffset issuedAt, out DateTimeOffset expiresAt)
        {
            issuedAt = DateTimeOffset.MinValue;
            expiresAt = DateTimeOffset.MinValue;

            if (string.IsNullOrWhiteSpace(jwt))
                return false;

            var parts = jwt.Split('.');
            if (parts.Length != 3)
                return false;

            try
            {
                // Decode the payload (second part)
                string payload = DecodeBase64Url(parts[1]);
                if (string.IsNullOrWhiteSpace(payload))
                    return false;

#if NET8_0_OR_GREATER
                var jsonPayload = JsonNode.Parse(payload)?.AsObject();
                if (jsonPayload == null)
                    return false;

                // Extract "exp" claim
                if (jsonPayload.TryGetPropertyValue("exp", out var expNode))
                {
                    long expSeconds = expNode.GetValue<long>();
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                }
                else
                {
                    return false;
                }

                // Extract "iat" claim
                if (jsonPayload.TryGetPropertyValue("iat", out var iatNode))
                {
                    long iatSeconds = iatNode.GetValue<long>();
                    issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatSeconds);
                }
                else
                {
                    // If iat is not present, default to now - 1 hour (common JWT lifetime)
                    issuedAt = expiresAt.AddHours(-1);
                }
#else
                var jsonPayload = JObject.Parse(payload);
                if (jsonPayload == null)
                    return false;

                // Extract "exp" claim
                if (jsonPayload.TryGetValue("exp", out var expToken))
                {
                    long expSeconds = expToken.Value<long>();
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                }
                else
                {
                    return false;
                }

                // Extract "iat" claim
                if (jsonPayload.TryGetValue("iat", out var iatToken))
                {
                    long iatSeconds = iatToken.Value<long>();
                    issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatSeconds);
                }
                else
                {
                    // If iat is not present, default to now - 1 hour (common JWT lifetime)
                    issuedAt = expiresAt.AddHours(-1);
                }
#endif

                return expiresAt > DateTimeOffset.MinValue;
            }
            catch
            {
                // Parsing failed; return false
                return false;
            }
        }

        /// <summary>
        /// Decodes a Base64Url encoded string.
        /// </summary>
        private static string DecodeBase64Url(string base64Url)
        {
            if (string.IsNullOrWhiteSpace(base64Url))
                return null;

            // Convert Base64Url to Base64
            string base64 = base64Url.Replace('-', '+').Replace('_', '/');

            // Add padding if necessary
            int mod = base64.Length % 4;
            if (mod > 0)
            {
                base64 += new string('=', 4 - mod);
            }

            byte[] bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
