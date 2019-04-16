// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace CommonCache.Test.Common
{
    public static class PreRegisteredApps
    {
        // Resources
        public const string MsGraph = "https://graph.microsoft.com";

        // https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ApplicationBlade/appId/f0e0429e-060c-42d3-9375-913eb7c7a62d/objectId/1e878bc0-962f-47d9-b85c-73a8f59edbdc
        public static AppCoordinates CommonCacheTestV1 =>
            new AppCoordinates(
                "f0e0429e-060c-42d3-9375-913eb7c7a62d",
                "72f988bf-86f1-41af-91ab-2d7cd011db47", // microsoft.com
                new Uri("urn:ietf:wg:oauth:2.0:oob"));
    }
}
