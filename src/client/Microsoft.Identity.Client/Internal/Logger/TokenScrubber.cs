// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

namespace Microsoft.Identity.Client.Internal.Logger
{
    /// <summary>
    /// Redacts opaque tokens that eSTS/CCS have tagged with a Highly Identifiable Token (HIT) tag
    /// from MSAL's own log output before the text reaches any logging sink.
    /// </summary>
    /// <remarks>
    /// eSTS/CCS embed the "EvoStsArtifacts" marker (MSA uses the literal "MsaArtifacts") inside the
    /// base64/base64url encoded token. Because the marker can appear at any of three byte-offset
    /// alignments once base64 encoded, all three renderings are matched. The marker sits between the
    /// token header and body, so a match is expanded both left and right over the token charset to
    /// cover the full token run before replacing it with <see cref="Placeholder"/>.
    /// </remarks>
    internal static class TokenScrubber
    {
        internal const string Placeholder = "[Redacted opaque token]";

        private const string DisableSwitchName = "Microsoft.Identity.Client.DisableOpaqueTokenScrubbing";

        // Base64 renderings of the "EvoStsArtifacts" prefix at the three possible byte-offset
        // alignments, plus the MSA literal "MsaArtifacts". Ordinal, case-sensitive.
        private static readonly string[] s_patterns = new[]
        {
            "RXZvU3RzQXJ0aWZhY3Rz", // offset 0
            "V2b1N0c0FydGlmYWN0c",  // offset 1
            "dm9TdHNBcnRpZmFjdH",   // offset 2
            "MsaArtifacts",         // MSA literal
        };

        private static readonly bool s_disabled = AppContext.TryGetSwitch(DisableSwitchName, out bool isDisabled) && isDisabled;

        /// <summary>
        /// Redacts any HIT-tagged opaque tokens found in <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The log message that may contain a tagged opaque token.</param>
        /// <returns>
        /// The same string reference when nothing was redacted (fast path, no allocation); otherwise a new
        /// string with each tagged token run replaced by <see cref="Placeholder"/>.
        /// </returns>
        /// <example>
        /// <code>
        /// string scrubbed = TokenScrubber.Scrub("Set-Cookie: esctx-x=AQAB...RXZvU3RzQXJ0aWZhY3Rz...; path=/");
        /// // scrubbed == "Set-Cookie: [Redacted opaque token]; path=/"
        /// </code>
        /// </example>
        public static string Scrub(string message)
        {
            if (s_disabled || string.IsNullOrEmpty(message))
            {
                return message;
            }

            // Single search for the first match. On the no-match fast path this returns the same
            // reference with no allocation and without scanning the string twice.
            int matchStart = FindEarliestMatch(message, 0, out int matchLength);
            if (matchStart < 0)
            {
                return message;
            }

            var sb = new StringBuilder(message.Length);
            int index = 0;

            while (matchStart >= 0)
            {
                // Expand the match left and right over the token charset to cover the whole run.
                int runStart = matchStart;
                while (runStart > index && IsTokenChar(message[runStart - 1]))
                {
                    runStart--;
                }

                int runEnd = matchStart + matchLength; // exclusive
                while (runEnd < message.Length && IsTokenChar(message[runEnd]))
                {
                    runEnd++;
                }

                // Append text before the run, then the placeholder.
                sb.Append(message, index, runStart - index);
                sb.Append(Placeholder);

                index = runEnd;
                matchStart = index < message.Length ? FindEarliestMatch(message, index, out matchLength) : -1;
            }

            if (index < message.Length)
            {
                sb.Append(message, index, message.Length - index);
            }

            return sb.ToString();
        }

        private static int FindEarliestMatch(string message, int startIndex, out int matchLength)
        {
            int earliest = -1;
            matchLength = 0;

            for (int i = 0; i < s_patterns.Length; i++)
            {
                int found = message.IndexOf(s_patterns[i], startIndex, StringComparison.Ordinal);
                if (found >= 0 && (earliest < 0 || found < earliest))
                {
                    earliest = found;
                    matchLength = s_patterns[i].Length;
                }
            }

            return earliest;
        }

        private static bool IsTokenChar(char c)
        {
            // base64 + base64url + '=' padding: [A-Za-z0-9+/_-=]
            return (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z') ||
                   (c >= '0' && c <= '9') ||
                   c == '+' || c == '/' || c == '_' || c == '-' || c == '=';
        }
    }
}
