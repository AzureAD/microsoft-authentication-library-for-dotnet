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
        private const string DisableEnvVar = "MSAL_MI_DISABLE_PERSISTENT_CERT_CACHE";

        [TestMethod]
        [DataRow("true")]
        [DataRow("TRUE")]
        [DataRow("1")]
        public void Factory_Disabled_ByEnvVar(string environmentVariableValue)
        {
            using (new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable(DisableEnvVar, environmentVariableValue);

                var logger = Substitute.For<ILoggerAdapter>();
                var cache = PersistentCertificateCacheFactory.Create(logger);

                Assert.IsInstanceOfType(cache, typeof(NoOpPersistentCertificateCache));
            }
        }

        [TestMethod]
        public void Factory_Defaults_To_Platform_When_EnvVar_Unset()
        {
            using (new EnvVariableContext())
            {
                // Ensure unset
                Environment.SetEnvironmentVariable(DisableEnvVar, null);

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
    }
}
