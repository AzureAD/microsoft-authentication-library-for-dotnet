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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Identity.Client.Internal
{
    internal static class MsalStringHelper
    {
        internal static HashSet<string> ToLower(this HashSet<string> setOfStrings)
        {
            if (setOfStrings == null)
            {
                return null;
            }

            HashSet<string> set = new HashSet<string>();
            foreach (var item in setOfStrings)
            {
                set.Add(item.ToLower());
            }

            return set;
        }

        internal static string[] AsArray(this HashSet<string> setOfStrings)
        {
            if (setOfStrings == null)
            {
                return null;
            }

            return setOfStrings.ToArray();
        }

        internal static string AsSingleString(this HashSet<string> setOfStrings)
        {
            return AsSingleString(setOfStrings.ToArray());
        }

        internal static string AsSingleString(this string[] arrayOfStrings)
        {

            if (IsNullOrEmpty(arrayOfStrings))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(arrayOfStrings[0]);

            for (int i = 1; i < arrayOfStrings.Length; i++)
            {
                sb.Append(" ");
                sb.Append(arrayOfStrings[i]);
            }

            return sb.ToString();
        }
        
        internal static HashSet<string> AsSet(this string singleString)
        {
            if (string.IsNullOrEmpty(singleString))
            {
                return new HashSet<string>();    
            }

            return new HashSet<string>(singleString.Split(new[] { " " }, StringSplitOptions.None));
        }

        internal static string[] AsArray(this string singleString)
        {
            if (string.IsNullOrWhiteSpace(singleString))
            {
                return new string[] { };
            }

            return singleString.Split(new[] { " " }, StringSplitOptions.None);
        }

        internal static HashSet<string> CreateSetFromArray(this string[] arrayStrings)
        {
            HashSet<string> set = new HashSet<string>();
            if (arrayStrings == null || arrayStrings.Length == 0)
            {
                return set;
            }

            foreach (string str in arrayStrings)
            {
                set.Add(str);
            }

            return set;
        }

        internal static bool IsNullOrEmpty(string[] input)
        {
            return input == null || input.Length == 0;
        }
    }
}
