// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Encodes/decodes the persisted X.509 FriendlyName for MSAL mTLS certs.
    /// Format: "MSAL|alias=cacheKey|ep=endpointBase"
    /// Open the cert store and look at FriendlyName to see examples.
    /// Wish we could paste a screenshot here... Maybe I can show it in code walkthroughs.
    /// </summary>
    internal static class FriendlyNameCodec
    {
        public const string Prefix = "MSAL|";
        public const string TagAlias = "alias";
        public const string TagEp = "ep";

        /// <summary>
        /// Encodes alias and endpointBase into friendly name.
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="endpointBase"></param>
        /// <param name="friendlyName"></param>
        /// <returns></returns>
        public static bool TryEncode(string alias, string endpointBase, out string friendlyName)
        {
            friendlyName = null;

            if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(endpointBase))
                return false;

            alias = alias.Trim();
            endpointBase = endpointBase.Trim();

            // Forbid characters that would break our simple delimiter-based grammar.
            if (ContainsIllegal(alias) || ContainsIllegal(endpointBase))
                return false;

            friendlyName = Prefix + TagAlias + "=" + alias + "|" + TagEp + "=" + endpointBase;
            return true;
        }

        /// <summary>
        /// Decodes friendly name into alias and endpointBase.
        /// </summary>
        /// <param name="friendlyName"></param>
        /// <param name="alias"></param>
        /// <param name="endpointBase"></param>
        /// <returns></returns>
        public static bool TryDecode(string friendlyName, out string alias, out string endpointBase)
        {
            alias = null;
            endpointBase = null;

            if (string.IsNullOrEmpty(friendlyName) ||
                !friendlyName.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            // Example: MSAL|alias=<cacheKey>|ep=<endpointBase>
            var payload = friendlyName.Substring(Prefix.Length);
            var parts = payload.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // Parse key-value pairs
            foreach (var part in parts)
            {
                var kv = part.Split(new[] { '=' }, 2);
                if (kv.Length != 2)
                    continue;

                var k = kv[0].Trim();
                var v = kv[1].Trim();

                if (k.Equals(TagAlias, StringComparison.Ordinal))
                {
                    alias = v;  // minimal: last-wins
                }
                else if (k.Equals(TagEp, StringComparison.Ordinal))
                {
                    endpointBase = v;
                }
            }

            return !string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(endpointBase);
        }

        /// <summary>
        /// Checks for illegal characters in alias/endpointBase.
        /// Endpoint itself comes from IMDS and is well-formed, but we still validate.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool ContainsIllegal(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '|' || c == '\r' || c == '\n' || c == '\0')
                    return true;
            }
            return false;
        }
    }
}
