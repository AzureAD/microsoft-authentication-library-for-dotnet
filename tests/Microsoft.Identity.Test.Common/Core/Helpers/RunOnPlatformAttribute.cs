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
        public RunOnOSXAttribute([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) 
            : base(OSPlatform.OSX, filePath, lineNumber)
        {
        }
    }

    public class RunOnWindowsAttribute : RunOnPlatformAttribute
    {
        public RunOnWindowsAttribute([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) 
            : base(OSPlatform.Windows, filePath, lineNumber)
        {
        }
    }

    public class RunOnLinuxAttribute : RunOnPlatformAttribute
    {
        public RunOnLinuxAttribute([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) 
            : base(OSPlatform.Linux, filePath, lineNumber)
        {
        }
    }

    public class DoNotRunOnWindowsAttribute : DoNotRunOnPlatformAttribute
    {
        public DoNotRunOnWindowsAttribute([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(OSPlatform.Windows, filePath, lineNumber)
        {

        }
    }

    public class DoNotRunOnLinuxAttribute : DoNotRunOnPlatformAttribute
    {
        public DoNotRunOnLinuxAttribute([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) 
            : base(OSPlatform.Linux, filePath, lineNumber)
        {
        }
    }

    public class RunOnPlatformAttribute : TestMethodAttribute
    {
        private readonly OSPlatform _platform;

        protected RunOnPlatformAttribute(OSPlatform platform, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
            _platform = platform;
        }

        public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
            if ((OsHelper.IsLinuxPlatform() && _platform != OSPlatform.Linux) ||
                (OsHelper.IsMacPlatform() && _platform != OSPlatform.OSX) ||
                (OsHelper.IsWindowsPlatform() && _platform != OSPlatform.Windows))
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

            return await base.ExecuteAsync(testMethod).ConfigureAwait(false);
        }
    }

    public class DoNotRunOnPlatformAttribute : TestMethodAttribute
    {
        private readonly OSPlatform _platform;

        protected DoNotRunOnPlatformAttribute(OSPlatform platform, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
            _platform = platform;
        }

        public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
            if ((OsHelper.IsLinuxPlatform() && _platform == OSPlatform.Linux) ||
                (OsHelper.IsMacPlatform() && _platform == OSPlatform.OSX) ||
                (OsHelper.IsWindowsPlatform() && _platform == OSPlatform.Windows))
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

            return await base.ExecuteAsync(testMethod).ConfigureAwait(false);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RunOnAzureDevOpsAttribute : TestMethodAttribute
    {
        public RunOnAzureDevOpsAttribute([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
        }

        public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
            // TF_BUILD is true for all Azure DevOps agents
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")))
            {
                return new[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestFailureException = new AssertInconclusiveException("Skipped outside Azure DevOps")
                    }
                };
            }

            return await base.ExecuteAsync(testMethod).ConfigureAwait(false);
        }
    }
}
