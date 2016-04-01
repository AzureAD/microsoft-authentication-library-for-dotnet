//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class Base64UrlEncoder
    {
        private const char Base64PadCharacter = '=';
        private const char Base64Character62 = '+';
        private const char Base64Character63 = '/';
        private const char Base64UrlCharacter62 = '-';
        private const char Base64UrlCharacter63 = '_';
        private static readonly Encoding TextEncoding = Encoding.UTF8;
        private static readonly string DoubleBase64PadCharacter = string.Format(CultureInfo.InvariantCulture, "{0}{0}", Base64PadCharacter);

        //
        // The following functions perform base64url encoding which differs from regular base64 encoding as follows
        // * padding is skipped so the pad character '=' doesn't have to be percent encoded
        // * the 62nd and 63rd regular base64 encoding characters ('+' and '/') are replace with ('-' and '_')
        // The changes make the encoding alphabet file and URL safe
        // See RFC4648, section 5 for more info
        //
        public static string Encode(string arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            return Encode(TextEncoding.GetBytes(arg));
        }

        public static byte[] DecodeBytes(string arg)
        {
            string s = arg;
            s = s.Replace(Base64UrlCharacter62, Base64Character62); // 62nd char of encoding
            s = s.Replace(Base64UrlCharacter63, Base64Character63); // 63rd char of encoding

            switch (s.Length % 4) 
            {
                // Pad 
                case 0:
                    break; // No pad chars in this case
                case 2:
                    s += DoubleBase64PadCharacter; 
                    break; // Two pad chars
                case 3:
                    s += Base64PadCharacter; 
                    break; // One pad char
                default:
                    throw new ArgumentException("Illegal base64url string!", "arg");
            }

            return Convert.FromBase64String(s); // Standard base64 decoder

        }

        internal static string Encode(byte[] arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            string s = Convert.ToBase64String(arg);
            s = s.Split(Base64PadCharacter)[0]; // Remove any trailing padding
            s = s.Replace(Base64Character62, Base64UrlCharacter62);  // 62nd char of encoding
            s = s.Replace(Base64Character63, Base64UrlCharacter63);  // 63rd char of encoding

            return s;
        }
    }
}
