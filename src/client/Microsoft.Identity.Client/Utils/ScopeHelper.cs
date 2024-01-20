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
        private const string DefaultSuffix = "/.default";

        public static string OrderScopesAlphabetically(string originalScopes)
        {
            // split by space and order alphabetically
            string[] split = originalScopes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            // order the scopes in alphabetical order
            Array.Sort(split, StringComparer.OrdinalIgnoreCase);
            return string.Join(" ", split);
        }


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

        public static string GetMsalRuntimeScopes()
        {
            return string.Join(" ", OAuth2Value.ReservedScopes);
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

        public static string ScopesToResource(string[] scopes)
        {
            if (scopes == null)
            {
                throw new MsalClientException(MsalError.ExactlyOneScopeExpected, MsalErrorMessage.ManagedIdentityExactlyOneScopeExpected);
            }

            if (scopes.Length != 1)
            {
                throw new MsalClientException(MsalError.ExactlyOneScopeExpected, MsalErrorMessage.ManagedIdentityExactlyOneScopeExpected);
            }

            if (!scopes[0].EndsWith(DefaultSuffix, StringComparison.Ordinal))
            {
                return scopes[0];
            }

            return scopes[0].Remove(scopes[0].LastIndexOf(DefaultSuffix, StringComparison.Ordinal));
        }

        public static string RemoveDefaultSuffixIfPresent(string resource)
        {
            if (!resource.EndsWith(DefaultSuffix, StringComparison.Ordinal))
            {
                return resource;
            }

            return resource.Remove(resource.LastIndexOf(DefaultSuffix, StringComparison.Ordinal));
        }
    }
}
