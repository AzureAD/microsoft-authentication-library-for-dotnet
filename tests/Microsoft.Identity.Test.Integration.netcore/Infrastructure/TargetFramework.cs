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
    /// Important: this class must be in a project that is built on: netfx, netcore and netstandard (i.e. not on Test.Common!)
    /// </summary>
    public class RunOnAttribute : TestMethodAttribute
    {
        private readonly TargetFrameworks _tfms;
        private readonly SkipConditions _skip;

        public RunOnAttribute(TargetFrameworks tfms, SkipConditions skipOn = SkipConditions.None, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
            _tfms = tfms;
            _skip = skipOn;
        }

        /// <summary>
        /// Use this overload when no framework targeting is needed — the test runs on all frameworks
        /// but should be skipped under the specified conditions.
        /// </summary>
        public RunOnAttribute(SkipConditions skipOn, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
            : this(TargetFrameworks.NetFx | TargetFrameworks.NetCore, skipOn, filePath, lineNumber)
        {
        }

        public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
            if (RunOnHelper.IsNetFwk() && (_tfms & TargetFrameworks.NetFx) != TargetFrameworks.NetFx ||
                RunOnHelper.IsNetCore() && (_tfms & TargetFrameworks.NetCore) != TargetFrameworks.NetCore)
            {
                return Inconclusive($"Skipped on target framework {_tfms}");
            }

            string skipReason = EvaluateSkipConditions(_skip);
            if (skipReason != null)
                return Inconclusive(skipReason);

            return await base.ExecuteAsync(testMethod).ConfigureAwait(false);
        }

        private static string EvaluateSkipConditions(SkipConditions conditions)
        {
            if (conditions == SkipConditions.None)
                return null;

#if ONEBRANCH_BUILD
            if ((conditions & SkipConditions.OneBranchBuild) != 0)
                return "Skipped on OneBranch pipeline";
#endif

            if ((conditions & SkipConditions.Linux) != 0 && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Skipped on Linux";

            if ((conditions & SkipConditions.Windows) != 0 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Skipped on Windows";

            if ((conditions & SkipConditions.macOS) != 0 && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "Skipped on macOS";

#if IGNORE_FEDERATED
            if ((conditions & SkipConditions.FederatedDisabled) != 0)
                return "Skipped: federated tests disabled";
#endif

            return null;
        }

        private static TestResult[] Inconclusive(string message) => new[]
        {
            new TestResult
            {
                Outcome = UnitTestOutcome.Inconclusive,
                TestFailureException = new AssertInconclusiveException(message)
            }
        };
    }

    /// <summary>
    /// Where we run integration tests. It's not worth running all tests on all frameworks
    /// </summary>
    [Flags]
    public enum TargetFrameworks
    {
        NetFx = 1,
        NetCore = 2
    }

    /// <summary>
    /// Conditions that, when matched, cause a <see cref="RunOnAttribute"/> to skip the test as inconclusive.
    /// Multiple conditions can be combined with the bitwise OR operator; if any condition is met the test is skipped.
    /// </summary>
    [Flags]
    public enum SkipConditions
    {
        /// <summary>No skip conditions.</summary>
        None              = 0,
        /// <summary>Skip when built with the ONEBRANCH_BUILD compile-time symbol (PipelineType=OneBranch).</summary>
        OneBranchBuild    = 1,
        /// <summary>Skip when running on Linux.</summary>
        Linux             = 2,
        /// <summary>Skip when running on Windows.</summary>
        Windows           = 4,
        /// <summary>Skip when running on macOS.</summary>
        macOS             = 8,
        /// <summary>Skip when built with the IGNORE_FEDERATED compile-time symbol.</summary>
        FederatedDisabled = 16
    }

    public static class RunOnHelper
    {
        public static void AssertFramework(this TargetFrameworks runOn)
        {
            if (IsNetFwk() && (runOn & TargetFrameworks.NetFx) != TargetFrameworks.NetFx)
            {
                Assert.Inconclusive("Test not configured to run on .Net Fx");
            }

            if (IsNetCore() && (runOn & TargetFrameworks.NetCore) != TargetFrameworks.NetCore)
            {
                Assert.Inconclusive("Test not configured to run on .Net Core");
            }
        }

        public static bool IsNetFwk()
        {
#if NETFRAMEWORK 
            return true;
#elif NET_CORE || NETSTANDARD
            return false;
#else
            throw new NotImplementedException();
#endif

        }

        public static bool IsNetCore()
        {
#if NETFRAMEWORK || NETSTANDARD
            return false;
#elif NET_CORE
            return true;
#else
            throw new NotImplementedException();
#endif

        }
    }
}
