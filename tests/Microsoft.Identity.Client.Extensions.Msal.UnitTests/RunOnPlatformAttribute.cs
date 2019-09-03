// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;

namespace Microsoft.Identity.Client.Extensions.Msal.UnitTests
{
    public class RunOnOSXAttribute : RunOnPlatformAttribute
    {
        public RunOnOSXAttribute() : base(OSPlatform.OSX)
        {
        }
    }

    public class RunOnWindowsAttribute : RunOnPlatformAttribute
    {
        public RunOnWindowsAttribute() : base(OSPlatform.Windows)
        {
        }
    }

    public class RunOnPlatformAttribute : TestMethodAttribute
    {
        private readonly OSPlatform _platform;

        protected RunOnPlatformAttribute(OSPlatform platform)
        {
            _platform = platform;
        }

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            if (!RuntimeInformation.IsOSPlatform(_platform))
            {
                return new[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestFailureException = new AssertInconclusiveException("Skipped on platform")
                    }
                };
            }

            return base.Execute(testMethod);
        }
    }
}
