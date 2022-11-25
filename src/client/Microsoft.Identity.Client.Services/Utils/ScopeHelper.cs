// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Utils
{
    internal static class ScopeHelper
    {
        public static bool ScopeContains(ISet<string> outerSet, IEnumerable<string> possibleContainedSet)
        {
            foreach (string key in possibleContainedSet)
            {
                if (!outerSet.Contains(key) && !string.IsNullOrEmpty(key))
                {
                    return false;
                }
            }

            return true;
        }

        public static HashSet<string> GetMsalScopes(HashSet<string> userScopes)
        {
            return new HashSet<string>(userScopes.Concat(OAuth2Value.ReservedScopes));
        }

        public static bool HasNonMsalScopes(HashSet<string> userScopes)
        {
            if (userScopes == null)
                return false;

            foreach (var userScope in userScopes)
            {
                if (!string.IsNullOrWhiteSpace(userScope) && 
                    !OAuth2Value.ReservedScopes.Contains(userScope))
                {
                    return true;
                }
            }
            return false;
        }

        public static HashSet<string> ConvertStringToScopeSet(string singleString)
        {
            if (string.IsNullOrEmpty(singleString))
            {
                return new HashSet<string>();
            }

            return new HashSet<string>(
                singleString.Split(' '), 
                StringComparer.OrdinalIgnoreCase);
        }

        public static HashSet<string> CreateScopeSet(IEnumerable<string> input)
        {
            if (input == null)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return new HashSet<string>(input, StringComparer.OrdinalIgnoreCase);
        }    
    }
}
