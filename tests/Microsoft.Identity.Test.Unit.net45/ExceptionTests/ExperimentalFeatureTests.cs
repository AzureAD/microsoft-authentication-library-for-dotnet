// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ExceptionTests
{
    [TestClass]
    public class ExperimentalFeatureTests
    {


        [TestMethod]
        public async Task ExperimentalFeatureExceptionAsync()
        {
            var pca = PublicClientApplicationBuilder.Create(Guid.NewGuid().ToString()).Build();
            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => pca.AcquireTokenInteractive(new[] { "scope" }).WithProofOfPosession(null).ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.ExperimentalFeature, ex.ErrorCode);
        }

    }
}
