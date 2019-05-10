// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
