// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// Test attribute that runs a test only on macOS (OSX); otherwise the test is skipped as inconclusive.
    /// </summary>
    public class RunOnOSXAttribute : RunOnPlatformAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunOnOSXAttribute"/> class.
        /// </summary>
        /// <param name="filePath">The source file path where the attribute is applied (supplied by the compiler).</param>
        /// <param name="lineNumber">The source line number where the attribute is applied (supplied by the compiler).</param>
        public RunOnOSXAttribute([CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
            : base(OSPlatform.OSX, filePath, lineNumber)
        {
        }
    }

    /// <summary>
    /// Test attribute that runs a test only on Windows; otherwise the test is skipped as inconclusive.
    /// </summary>
    public class RunOnWindowsAttribute : RunOnPlatformAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunOnWindowsAttribute"/> class.
        /// </summary>
        /// <param name="filePath">The source file path where the attribute is applied (supplied by the compiler).</param>
        /// <param name="lineNumber">The source line number where the attribute is applied (supplied by the compiler).</param>
        public RunOnWindowsAttribute([CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
            : base(OSPlatform.Windows, filePath, lineNumber)
        {
        }
    }

    /// <summary>
    /// Test attribute that runs a test only on Linux; otherwise the test is skipped as inconclusive.
    /// </summary>
    public class RunOnLinuxAttribute : RunOnPlatformAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunOnLinuxAttribute"/> class.
        /// </summary>
        /// <param name="filePath">The source file path where the attribute is applied (supplied by the compiler).</param>
        /// <param name="lineNumber">The source line number where the attribute is applied (supplied by the compiler).</param>
        public RunOnLinuxAttribute([CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
            : base(OSPlatform.Linux, filePath, lineNumber)
        {
        }
    }

    /// <summary>
    /// Test attribute that skips execution on Windows.
    /// </summary>
    public class DoNotRunOnWindowsAttribute : DoNotRunOnPlatformAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoNotRunOnWindowsAttribute"/> class.
        /// </summary>
        /// <param name="filePath">The source file path where the attribute is applied (supplied by the compiler).</param>
        /// <param name="lineNumber">The source line number where the attribute is applied (supplied by the compiler).</param>
        public DoNotRunOnWindowsAttribute([CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
            : base(OSPlatform.Windows, filePath, lineNumber)
        {
        }
    }

    /// <summary>
    /// Test attribute that skips execution on Linux.
    /// </summary>
    public class DoNotRunOnLinuxAttribute : DoNotRunOnPlatformAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoNotRunOnLinuxAttribute"/> class.
        /// </summary>
        /// <param name="filePath">The source file path where the attribute is applied (supplied by the compiler).</param>
        /// <param name="lineNumber">The source line number where the attribute is applied (supplied by the compiler).</param>
        public DoNotRunOnLinuxAttribute([CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
            : base(OSPlatform.Linux, filePath, lineNumber)
        {
        }
    }

    /// <summary>
    /// Base test attribute that conditionally runs or skips a test based on the current OS platform.
    /// </summary>
    public class RunOnPlatformAttribute : TestMethodAttribute
    {
        private readonly OSPlatform _platform;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunOnPlatformAttribute"/> class.
        /// </summary>
        /// <param name="platform">The platform used to evaluate whether to run or skip the test.</param>
        /// <param name="filePath">The source file path where the attribute is applied (supplied by the compiler).</param>
        /// <param name="lineNumber">The source line number where the attribute is applied (supplied by the compiler).</param>
        protected RunOnPlatformAttribute(OSPlatform platform, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
            _platform = platform;
        }

        /// <summary>
        /// Executes the test method or returns an inconclusive result when the current environment does not satisfy the attribute constraints.
        /// </summary>
        /// <param name="testMethod">The test method to execute.</param>
        /// <returns>A set of <see cref="TestResult"/> values for the executed test method.</returns>
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

    /// <summary>
    /// Base test attribute that conditionally skips a test when running on a specified OS platform.
    /// </summary>
    public class DoNotRunOnPlatformAttribute : TestMethodAttribute
    {
        private readonly OSPlatform _platform;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoNotRunOnPlatformAttribute"/> class.
        /// </summary>
        /// <param name="platform">The platform used to evaluate whether to run or skip the test.</param>
        /// <param name="filePath">The source file path where the attribute is applied (supplied by the compiler).</param>
        /// <param name="lineNumber">The source line number where the attribute is applied (supplied by the compiler).</param>
        protected DoNotRunOnPlatformAttribute(OSPlatform platform, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
            _platform = platform;
        }

        /// <summary>
        /// Executes the test method or returns an inconclusive result when the current environment does not satisfy the attribute constraints.
        /// </summary>
        /// <param name="testMethod">The test method to execute.</param>
        /// <returns>A set of <see cref="TestResult"/> values for the executed test method.</returns>
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

    /// <summary>
    /// Runs a test only when executing in an Azure DevOps environment; otherwise the test is skipped as inconclusive.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RunOnAzureDevOpsAttribute : TestMethodAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunOnAzureDevOpsAttribute"/> class.
        /// </summary>
        /// <param name="filePath">The source file path where the attribute is applied (supplied by the compiler).</param>
        /// <param name="lineNumber">The source line number where the attribute is applied (supplied by the compiler).</param>
        public RunOnAzureDevOpsAttribute([CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
        }

        /// <summary>
        /// Executes the test method or returns an inconclusive result when the current environment does not satisfy the attribute constraints.
        /// </summary>
        /// <param name="testMethod">The test method to execute.</param>
        /// <returns>A set of <see cref="TestResult"/> values for the executed test method.</returns>
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
