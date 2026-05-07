// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// JWT claim extractor leveraging MSAL's existing helper utilities.
    /// </summary>
    internal static class JwtClaimExtractor
    {
        /// <summary>
        /// Extracts the 'exp' (expiration) claim from a JWT payload.
        /// </summary>
        /// <param name="jwt">The JWT string (format: header.payload.signature).</param>
        /// <param name="expiresOn">The parsed expiration time in UTC, or DateTimeOffset.MinValue on failure.</param>
        /// <returns>True if the exp claim was successfully extracted; false otherwise.</returns>
        internal static bool TryExtractExpirationClaim(string jwt, out DateTimeOffset expiresOn)
        {
            expiresOn = DateTimeOffset.MinValue;

            try
            {
                // Split JWT into parts
                var parts = jwt.Split('.');
                if (parts.Length < 2)
                    return false;

                // Use MSAL's Base64UrlHelpers to decode the payload (handles padding automatically)
                string payloadJson = Base64UrlHelpers.Decode(parts[1]);

                if (string.IsNullOrEmpty(payloadJson))
                    return false;

                // Use MSAL's JsonHelper to parse JSON (handles both System.Text.Json and Newtonsoft)
                var claims = JsonHelper.DeserializeFromJson<Dictionary<string, object>>(payloadJson);

                if (claims == null || !claims.TryGetValue("exp", out object expObj))
                    return false;

                // Parse the exp claim (Unix timestamp in seconds)
                // STJ deserializes numbers in Dictionary<string,object> as JsonElement, not long
                if (expObj is JsonElement jsonElement)
                {
                    if (jsonElement.TryGetInt64(out long elementLong))
                    {
                        expiresOn = DateTimeOffset.FromUnixTimeSeconds(elementLong);
                        return true;
                    }
                    // Handle number-as-string in JSON
                    if (jsonElement.ValueKind == JsonValueKind.String &&
                        long.TryParse(jsonElement.GetString(), out long parsedFromElement))
                    {
                        expiresOn = DateTimeOffset.FromUnixTimeSeconds(parsedFromElement);
                        return true;
                    }
                }

                // Fallback: direct long (shouldn't happen with STJ but kept for safety)
                if (expObj is long expLong)
                {
                    expiresOn = DateTimeOffset.FromUnixTimeSeconds(expLong);
                    return true;
                }

                // Handle case where exp comes as string (defensive)
                if (expObj is string expStr && long.TryParse(expStr, out long parsedExp))
                {
                    expiresOn = DateTimeOffset.FromUnixTimeSeconds(parsedExp);
                    return true;
                }
            }
            catch
            {
                // Silently fail: malformed JWT or parsing error
            }

            return false;
        }
    }
}
