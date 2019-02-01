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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests.Harnesses
{
    internal abstract class AbstractBuilderHarness
    {
        public AcquireTokenCommonParameters CommonParametersReceived { get; protected set; }

        public void ValidateCommonParameters(
            ApiEvent.ApiIds expectedApiId,
            string expectedAuthorityOverride = null,
            Dictionary<string, string> expectedExtraQueryParameters = null,
            IEnumerable<string> expectedScopes = null)
        {
            Assert.IsNotNull(CommonParametersReceived);

            Assert.AreEqual(expectedApiId, CommonParametersReceived.ApiId);
            Assert.AreEqual(expectedAuthorityOverride, CommonParametersReceived.AuthorityOverride);
            CoreAssert.AreScopesEqual(
                (expectedScopes ?? MsalTestConstants.Scope).AsSingleString(),
                CommonParametersReceived.Scopes.AsSingleString());
            CollectionAssert.AreEqual(
                expectedExtraQueryParameters, 
                CommonParametersReceived.ExtraQueryParameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
    }
}