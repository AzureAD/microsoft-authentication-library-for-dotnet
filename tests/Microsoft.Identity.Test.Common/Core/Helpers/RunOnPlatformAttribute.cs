// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public class RunOnOSXAttribute : RunOnPlatformAttribute
    {
        public RunOnOSXAttribute(
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1)
            : base(OSPlatform.OSX, callerFilePath, callerLineNumber)
        {
        }
    }

    public class RunOnWindowsAttribute : RunOnPlatformAttribute
    {
        public RunOnWindowsAttribute(
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1)
            : base(OSPlatform.Windows, callerFilePath, callerLineNumber)
        {
        }
    }

    public class RunOnLinuxAttribute : RunOnPlatformAttribute
    {
        public RunOnLinuxAttribute(
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1)
            : base(OSPlatform.Linux, callerFilePath, callerLineNumber)
        {
        }
    }

    public class DoNotRunOnWindowsAttribute : DoNotRunOnPlatformAttribute
    {
        public DoNotRunOnWindowsAttribute(
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1)
            : base(OSPlatform.Windows, callerFilePath, callerLineNumber)
        {
        }
    }

    public class DoNotRunOnLinuxAttribute : DoNotRunOnPlatformAttribute
    {
        public DoNotRunOnLinuxAttribute(
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1)
            : base(OSPlatform.Linux, callerFilePath, callerLineNumber)
        {
        }
    }

    public class RunOnPlatformAttribute : TestMethodAttribute
    {
        private readonly OSPlatform _platform;

        protected RunOnPlatformAttribute(
            OSPlatform platform,
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1)
            : base(callerFilePath, callerLineNumber) // MSTEST0057
        {
            _platform = platform;
        }

        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod) // MSTest v4 signature
        {
            if ((OsHelper.IsLinuxPlatform() && _platform != OSPlatform.Linux) ||
                (OsHelper.IsMacPlatform() && _platform != OSPlatform.OSX) ||
                (OsHelper.IsWindowsPlatform() && _platform != OSPlatform.Windows))
            {
                return Task.FromResult(new[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestFailureException = new AssertInconclusiveException("Skipped on platform")
                    }
                });
            }

            return base.ExecuteAsync(testMethod);
        }
    }

    public class DoNotRunOnPlatformAttribute : TestMethodAttribute
    {
        private readonly OSPlatform _platform;

        protected DoNotRunOnPlatformAttribute(
            OSPlatform platform,
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1)
            : base(callerFilePath, callerLineNumber) // MSTEST0057
        {
            _platform = platform;
        }

        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod) // MSTest v4 signature
        {
            if ((OsHelper.IsLinuxPlatform() && _platform == OSPlatform.Linux) ||
                (OsHelper.IsMacPlatform() && _platform == OSPlatform.OSX) ||
                (OsHelper.IsWindowsPlatform() && _platform == OSPlatform.Windows))
            {
                return Task.FromResult(new[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestFailureException = new AssertInconclusiveException("Skipped on platform")
                    }
                });
            }

            return base.ExecuteAsync(testMethod);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RunOnAzureDevOpsAttribute : TestMethodAttribute
    {
        public RunOnAzureDevOpsAttribute(
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1)
            : base(callerFilePath, callerLineNumber) // MSTEST0057
        {
        }

        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod) // MSTest v4 signature
        {
            // TF_BUILD is true for all Azure DevOps agents
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")))
            {
                return Task.FromResult(new[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestFailureException = new AssertInconclusiveException("Skipped outside Azure DevOps")
                    }
                });
            }

            return base.ExecuteAsync(testMethod);
        }
    }
}
