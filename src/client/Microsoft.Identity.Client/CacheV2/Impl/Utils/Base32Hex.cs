// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Text;

namespace Microsoft.Identity.Client.CacheV2.Impl.Utils
{
    /// <summary>
    /// A class to implement Base32Hex encoding
    /// </summary>
    internal static class Base32Hex
    {
        /// <summary>
        ///     The different characters allowed in Base32 encoding.
        /// </summary>
        /// <remarks>
        ///     This is a 32-character subset of the twenty-six letters A–Z and six digits 2–7.
        ///     https://en.wikipedia.org/wiki/Base32
        /// </remarks>
        // private const string Base32AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private const string Base32HexAllowedCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        /// <summary>
        ///     Converts a byte array into a Base32 string.
        /// </summary>
        /// <param name="input">The string to convert to Base32.</param>
        /// <param name="addPadding">Whether or not to add RFC3548 '='-padding to the string.</param>
        /// <returns>A Base32 string.</returns>
        /// <remarks>
        ///     https://tools.ietf.org/html/rfc3548#section-2.2 indicates padding MUST be added unless the reference to the RFC
        ///     tells us otherswise.
        ///     https://github.com/google/google-authenticator/wiki/Key-Uri-Format indicates that padding SHOULD be omitted.
        ///     To meet both requirements, you can omit padding when required.
        /// </remarks>
        public static string ToBase32String(byte[] input, bool addPadding = true)
        {
            if (input == null || input.Length == 0)
            {
                return string.Empty;
            }

            string bits = input.Select(b => Convert.ToString((byte)b, 2).PadLeft(8, '0')).Aggregate((a, b) => a + b)
                               .PadRight((int)(Math.Ceiling(input.Length * 8 / 5d) * 5), '0');
            string result = Enumerable.Range(0, bits.Length / 5)
                                      .Select(
                                          i => Base32HexAllowedCharacters.Substring(
                                              Convert.ToInt32(bits.Substring(i * 5, 5), 2),
                                              1)).Aggregate((a, b) => a + b);
            if (addPadding)
            {
                result = result.PadRight((int)(Math.Ceiling(result.Length / 8d) * 8), '=');
            }

            return result;
        }

        public static string EncodeAsBase32String(string input, bool addPadding = true)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(input);
            string result = ToBase32String(bytes, addPadding);
            return result;
        }

        public static string DecodeFromBase32String(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            byte[] bytes = input.ToByteArray();
            string result = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            return result;
        }

        /// <summary>
        ///     Converts a Base32 string into the corresponding byte array, using 5 bits per character.
        /// </summary>
        /// <param name="input">The Base32 String</param>
        /// <returns>A byte array containing the properly encoded bytes.</returns>
        public static byte[] ToByteArray(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new byte[0];
            }

            string bits = input.TrimEnd('=').ToUpperInvariant().ToCharArray()
                               .Select(c => Convert.ToString(Base32HexAllowedCharacters.IndexOf(c), 2).PadLeft(5, '0'))
                               .Aggregate((a, b) => a + b);
            byte[] result = Enumerable.Range(0, bits.Length / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, 8), 2))
                                      .ToArray();
            return result;
        }
    }
}
