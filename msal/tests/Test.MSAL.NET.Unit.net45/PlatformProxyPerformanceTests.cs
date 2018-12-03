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
using Microsoft.Identity.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Common.Core.Helpers;

namespace Test.MSAL.NET.Unit.net45
{
    [TestClass]
    public class PlatformProxyPerformanceTests
    {
        [TestMethod]
        public void ValidateGetPlatformProxyPerformance()
        {
            using (new PerformanceValidator(50, "GetPlatformProxy"))
            {
                PlatformProxyFactory.GetPlatformProxy();
            }
        }

        private void ValidateMethodPerformance(long maxMilliseconds, string name, Action<IPlatformProxy> action)
        {
            var platformProxy = PlatformProxyFactory.GetPlatformProxy();

            // Call it once to pre-load it.  We're not worried about the time it takes to call it
            // the first time, we're worried about subsequent calls.
            action(platformProxy);

            using (new PerformanceValidator(maxMilliseconds, name))
            {
                action(platformProxy);
            }
        }

        [TestMethod]
        public void ValidateGetDeviceModelPerformance()
        {
            ValidateMethodPerformance(2, "GetDeviceModel", proxy => proxy.GetDeviceModel());
        }

        [TestMethod]
        public void ValidateGetDeviceIdPerformance()
        {
            ValidateMethodPerformance(2, "GetDeviceId", proxy => proxy.GetDeviceId());
        }

        [TestMethod]
        public void ValidateGetOperatingSystemPerformance()
        {
            ValidateMethodPerformance(2, "GetOperatingSystem", proxy => proxy.GetOperatingSystem());
        }

        [TestMethod]
        public void ValidateGetProcessorArchitecturePerformance()
        {
            ValidateMethodPerformance(2, "GetProcessorArchitecture", proxy => proxy.GetProcessorArchitecture());
        }

        [TestMethod]
        public void ValidateIsDomainJoinedPerformance()
        {
            ValidateMethodPerformance(2, "IsDomainJoined", proxy => proxy.IsDomainJoined());
        }

        [TestMethod]
        public void ValidateGetCallingApplicationNamePerformance()
        {
            ValidateMethodPerformance(2, "GetCallingApplicationName", proxy => proxy.GetCallingApplicationName());
        }

        [TestMethod]
        public void ValidateGetCallingApplicationVersionPerformance()
        {
            ValidateMethodPerformance(2, "GetCallingApplicationVersion", proxy => proxy.GetCallingApplicationVersion());
        }

        [TestMethod]
        public void ValidateGetProductNamePerformance()
        {
            ValidateMethodPerformance(2, "GetProductName", proxy => proxy.GetProductName());
        }
    }
}