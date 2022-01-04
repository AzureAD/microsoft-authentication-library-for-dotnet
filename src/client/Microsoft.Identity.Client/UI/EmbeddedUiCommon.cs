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
        public static bool IsAllowedIeOrEdgeAuthorizationUri(Uri uri)
        {
            return uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
                uri.AbsoluteUri.Equals("about:blank", StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals("javascript", StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals("res", StringComparison.OrdinalIgnoreCase); // IE error pages
        }

    }
}
