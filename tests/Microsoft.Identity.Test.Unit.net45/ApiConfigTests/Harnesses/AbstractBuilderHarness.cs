// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
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
                (expectedScopes ?? TestConstants.s_scope).AsSingleString(),
                CommonParametersReceived.Scopes.AsSingleString());

            CollectionAssert.AreEqual(
                expectedExtraQueryParameters,
                CommonParametersReceived.ExtraQueryParameters?.ToList());
        }
    }
}
