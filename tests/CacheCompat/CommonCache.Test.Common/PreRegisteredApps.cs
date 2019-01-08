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