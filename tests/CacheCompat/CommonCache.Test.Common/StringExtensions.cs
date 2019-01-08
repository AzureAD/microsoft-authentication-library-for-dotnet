// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Security;

namespace CommonCache.Test.Common
{
    public static class StringExtensions
    {
        private static string EncloseWithString(
            this string val,
            char leftCharToEnclose,
            char rightCharToEnclose,
            bool forceEnclose)
        {
            string newVal = null;
            if (val != null)
            {
                newVal = val.Trim();
            }

            if (string.IsNullOrEmpty(newVal))
            {
                return $"{leftCharToEnclose}{rightCharToEnclose}";
            }

            if (forceEnclose)
            {
                return $"{leftCharToEnclose}{newVal}{rightCharToEnclose}";
            }

            if (newVal[0] == leftCharToEnclose)
            {
                if (newVal[newVal.Length - 1] != rightCharToEnclose)
                {
                    throw new ArgumentException(
                        $"String already starts with a {leftCharToEnclose} but doesn't end with {rightCharToEnclose}.");
                }

                // String starts and ends with charToEnclose
                return newVal;
            }

            if (newVal[newVal.Length - 1] == rightCharToEnclose)
            {
                throw new ArgumentException(
                    $"String doesn't start with a {leftCharToEnclose} but already ends with {rightCharToEnclose}.");
            }

            return $"{leftCharToEnclose}{newVal}{rightCharToEnclose}";
        }

        private static string EncloseWithString(this string val, char charToEnclose, bool forceEnclose)
        {
            return EncloseWithString(val, charToEnclose, charToEnclose, forceEnclose);
        }

        public static string EncloseQuotes(this string val)
        {
            return val.EncloseWithString('\"', false);
        }

        public static SecureString ToSecureString(this string val)
        {
            var secureString = new SecureString();
            val.ToCharArray().ToList().ForEach(c => secureString.AppendChar(c));
            return secureString;
        }
    }
}