// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Enable parallel test execution with 4 workers at the class level
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.ClassLevel)]

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    [TestClass]
    public class TestInitialization
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            // Initialize resources that need to be shared across tests
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            // Clean up shared resources
            WebDriverPool.Instance.Dispose();
        }
    }
}
