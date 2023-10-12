// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class AdfsAuthorityTests
    {
        [DataTestMethod]
        [DataRow("https://someAdfs.com/a dfs/")]
        [DataRow("http://someAdfs.com/adfs/")]
        public void MalformedAuthority_ThrowsException(string malformedAuthority)
        {
            Assert.ThrowsException<ArgumentException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAdfsAuthority(malformedAuthority)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build());

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .Build();

            Assert.ThrowsException<ArgumentException>(() =>
                app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                   .WithAdfsAuthority(malformedAuthority));
        }
    }
}
