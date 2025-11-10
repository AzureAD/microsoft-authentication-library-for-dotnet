// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Encodes/decodes the X.509 <c>FriendlyName</c> used by MSAL for mTLS-bound certificates.
    /// Best-effort only: methods are non-throwing so certificate persistence never blocks auth.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Format (v1):</b>
    /// <c>MSAL|alias=&lt;alias&gt;|ep=&lt;scheme&gt;://&lt;host&gt;[/&lt;tenant&gt;]</c>
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Values are unescaped and must not contain <c>|</c>, carriage return, line feed, or NULL.
    ///       (If any are present, the encoder returns <c>false</c> and persistence is skipped.)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Keys are lowercase as shown. Unknown <c>key=value</c> pairs may appear after <c>ep=</c>
    ///       and are ignored by the decoder for forward compatibility.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Case is preserved for values. No whitespace is added around separators.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // User-assigned MI via full ARM resource ID + tenant GUID
    /// MSAL|alias=/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/UAMI-2|ep=https://mtls.login/72f988bf-86f1-41af-91ab-2d7cd011db47
    /// </code>
    /// </example>
    /// <example>
    /// <code>
    /// // User-assigned MI expressed as ClientId/ObjectId
    /// MSAL|alias=8f123456-1f2e-4f3d-9e5b-9b9b9b9b9b9b|ep=https://mtls.login/contoso.onmicrosoft.com
    /// </code>
    /// </example>
    internal static class MsiCertificateFriendlyNameEncoder
    {
        public const string Prefix = "MSAL|";
        public const string TagAlias = "alias";
        public const string TagEp = "ep";

        /// <summary>
        /// Encodes alias and endpointBase into friendly name.
        /// Returns false on invalid input. We do not want to throw from here.
        /// Because persistent store is best-effort.
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
        /// Returns false on invalid input. We do not want to throw from here.
        /// Because persistent store is best-effort.
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

            // Example: MSAL|alias=ManagedIdentityId|ep=https://mtls.login/1234-tenant
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
        /// Returns true if illegal characters are found.
        /// </summary>
        /// <param name="value"></param>
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
