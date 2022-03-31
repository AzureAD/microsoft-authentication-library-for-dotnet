// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class PlatformProxyPerformanceTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void ValidateGetPlatformProxyPerformance()
        {
            using (new PerformanceValidator(200, "GetPlatformProxy"))
            {
                PlatformProxyFactory.CreatePlatformProxy(null);
            }
        }

        private void ValidateMethodPerformance(long maxMilliseconds, string name, Action<IPlatformProxy> action)
        {
            var platformProxy = PlatformProxyFactory.CreatePlatformProxy(null);

            // Call it once to pre-load it.  We're not worried about the time it takes to call it
            // the first time, we're worried about subsequent calls.
            action(platformProxy);

            using (new PerformanceValidator(maxMilliseconds, name))
            {
                action(platformProxy);
            }
        }

        private const long AllowedMilliseconds = 20;
        private const long DomainJoinedAllowedMilliseconds = 100;

        [TestMethod]
        public void ValidateGetDeviceModelPerformance()
        {
            ValidateMethodPerformance(AllowedMilliseconds, "GetDeviceModel", proxy => proxy.GetDeviceModel());
        }

        [TestMethod]
        public void ValidateGetDeviceIdPerformance()
        {
            ValidateMethodPerformance(AllowedMilliseconds, "GetDeviceId", proxy => proxy.GetDeviceId());
        }

        [TestMethod]
        public void ValidateGetOperatingSystemPerformance()
        {
            ValidateMethodPerformance(AllowedMilliseconds, "GetOperatingSystem", proxy => proxy.GetOperatingSystem());
        }

        [TestMethod]
        public void ValidateGetProcessorArchitecturePerformance()
        {
            ValidateMethodPerformance(AllowedMilliseconds, "GetProcessorArchitecture", proxy => proxy.GetProcessorArchitecture());
        }

        [TestMethod]
        public void ValidateGetCallingApplicationNamePerformance()
        {
            ValidateMethodPerformance(AllowedMilliseconds, "GetCallingApplicationName", proxy => proxy.GetCallingApplicationName());
        }

        [TestMethod]
        public void ValidateGetCallingApplicationVersionPerformance()
        {
            ValidateMethodPerformance(AllowedMilliseconds, "GetCallingApplicationVersion", proxy => proxy.GetCallingApplicationVersion());
        }

        [TestMethod]
        public void ValidateGetProductNamePerformance()
        {
            ValidateMethodPerformance(AllowedMilliseconds, "GetProductName", proxy => proxy.GetProductName());
        }
    }
}
