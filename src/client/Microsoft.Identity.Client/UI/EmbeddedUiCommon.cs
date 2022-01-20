// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.UI
{
    internal static class EmbeddedUiCommon
    {
        /// <summary>
        /// Validates that the authorization redirects do not happen over http or other insecure protocol.
        /// This does not include the final redirect, denoted by the redirect URI.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static bool IsAllowedIeOrEdgeAuthorizationRedirect(Uri uri)
        {
            return uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
                uri.AbsoluteUri.Equals("about:blank", StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals("javascript", StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals("res", StringComparison.OrdinalIgnoreCase); // IE error pages
        }
    }
}
