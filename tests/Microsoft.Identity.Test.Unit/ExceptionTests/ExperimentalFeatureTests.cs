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
        private static readonly string[] s_scopes = ["scope"];
#if NETFRAMEWORK
        [TestMethod]
        public async Task ExperimentalFeatureExceptionAsync()
        {
            IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                .Create(Guid.NewGuid().ToString())
                .WithCertificate(CertHelper.GetOrCreateTestCert()).Build();

            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => cca.AcquireTokenForClient(s_scopes).WithMtlsProofOfPossession().ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.ExperimentalFeature, ex.ErrorCode);
        }
#endif
    }
}
