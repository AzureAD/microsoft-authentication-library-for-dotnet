// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class PersistentCertificateCacheFactoryTests
    {
        private const string EnableEnvVar = "MSAL_MI_ENABLE_PERSISTENT_CERT_CACHE";

        [TestMethod]
        [DataRow("true")]
        [DataRow("TRUE")]
        [DataRow("TruE")]
        [DataRow("1")]
        [DataRow(" true ")]
        [DataRow("\t1\t")]
        public void Factory_Enabled_ByEnvVar(string environmentVariableValue)
        {
            using (new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable(EnableEnvVar, environmentVariableValue);

                var logger = Substitute.For<ILoggerAdapter>();
                var cache = PersistentCertificateCacheFactory.Create(logger);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Assert.IsInstanceOfType(cache, typeof(WindowsPersistentCertificateCache));
                }
                else
                {
                    Assert.IsInstanceOfType(cache, typeof(NoOpPersistentCertificateCache));
                }
            }
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Factory_Defaults_To_NoOp_When_EnvVar_Unset(string environmentVariableValue)
        {
            using (new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable(EnableEnvVar, environmentVariableValue);

                var logger = Substitute.For<ILoggerAdapter>();
                var cache = PersistentCertificateCacheFactory.Create(logger);

                Assert.IsInstanceOfType(cache, typeof(NoOpPersistentCertificateCache));
            }
        }

        [TestMethod]
        [DataRow("0")]
        [DataRow("false")]
        [DataRow("False")]
        [DataRow("FALSE")]
        [DataRow("yes")]
        [DataRow("no")]
        [DataRow("on")]
        [DataRow("off")]
        [DataRow("enabled")]
        [DataRow("ture")]
        public void Factory_Ignores_Unrecognized_Values(string environmentVariableValue)
        {
            using (new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable(EnableEnvVar, environmentVariableValue);

                var logger = Substitute.For<ILoggerAdapter>();
                var cache = PersistentCertificateCacheFactory.Create(logger);

                Assert.IsInstanceOfType(cache, typeof(NoOpPersistentCertificateCache));
            }
        }
    }
}
