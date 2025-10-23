// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// FriendlyName schema (compact, max 64 chars ):
    ///   MSAL|1|token_type|b64url-mtls_endpoint
    /// token_type: "bearer" or "mtls_pop"
    /// We don't need to add MI Id / Tenant here — they are already present in the subject (CN/DC).
    /// </summary>
    internal static class BindingFriendlyName
    {
        internal const string Prefix = "MSAL";
        internal const int SchemaVersion = 1;

        public static string Build(string tokenType, string mtlsEndpoint)
        {
            if (string.IsNullOrEmpty(tokenType))
                throw new ArgumentNullException(nameof(tokenType));
            if (string.IsNullOrEmpty(mtlsEndpoint))
                throw new ArgumentNullException(nameof(mtlsEndpoint));

            string endpointB64 = Base64UrlEncode(mtlsEndpoint);
            return $"{Prefix}|{SchemaVersion}|{tokenType}|{endpointB64}";
        }

        public static bool TryParse(
            string friendlyName,
            out string tokenType,
            out string mtlsEndpoint)
        {
            tokenType = null;
            mtlsEndpoint = null;

            if (string.IsNullOrEmpty(friendlyName))
                return false;

            var parts = friendlyName.Split('|');
            if (parts.Length != 4)
                return false;
            if (!StringComparer.Ordinal.Equals(parts[0], Prefix))
                return false;

            // Accept "1" and legacy "v1" for resilience.
            if (!(parts[1] == SchemaVersion.ToString() || parts[1] == "v1"))
                return false;

            tokenType = parts[2];
            mtlsEndpoint = Base64UrlDecode(parts[3]);
            return !string.IsNullOrEmpty(tokenType) && !string.IsNullOrEmpty(mtlsEndpoint);
        }

        public static bool HasOurPrefix(string friendlyName) =>
            !string.IsNullOrEmpty(friendlyName) &&
            friendlyName.StartsWith(Prefix + "|", StringComparison.Ordinal);

        private static string Base64UrlEncode(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s ?? "");
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string Base64UrlDecode(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            string b64 = s.Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4)
            {
                case 2:
                    b64 += "==";
                    break;
                case 3:
                    b64 += "=";
                    break;
            }
            var bytes = Convert.FromBase64String(b64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
