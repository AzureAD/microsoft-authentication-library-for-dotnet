// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET_CORE

using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Broker
{
    /// <summary>
    /// Temporary demonstration tests for the attribute ordering bug between
    /// [IgnoreOnOneBranch] and platform attributes like [DoNotRunOnLinux].
    ///
    /// Root cause: MSTest uses only the *first* TestMethodAttribute-derived attribute
    /// on a method as its executor. When [DoNotRunOnLinux] is listed before
    /// [IgnoreOnOneBranch], the platform check runs but the OneBranch skip never fires.
    ///
    /// HOW TO REPRODUCE:
    ///   Build with: dotnet build -p:PipelineType=OneBranch
    ///   Run with:   dotnet test --no-build
    ///
    /// EXPECTED RESULTS (OneBranch build, Windows agent):
    ///   CorrectOrder_IgnoreOnOneBranchFirst  => NotExecuted (skipped correctly)
    ///   WrongOrder_DoNotRunOnLinuxFirst      => Failed      (bug: skip was shadowed)
    ///
    /// EXPECTED RESULTS (normal build, no ONEBRANCH_BUILD):
    ///   Both tests fail - this is intentional; they exist only to demonstrate the bug.
    /// </summary>
    [TestClass]
    public class AttributeOrderingBugDemoTests
    {
        /// <summary>
        /// Correct ordering: [IgnoreOnOneBranch] first.
        /// On an OneBranch build this test should be skipped (NotExecuted) and
        /// never reach the Assert.Fail below.
        /// </summary>
        [IgnoreOnOneBranch]
        [DoNotRunOnLinux]
        public void CorrectOrder_IgnoreOnOneBranchFirst_ShouldBeSkippedOnOneBranch()
        {
            Assert.Fail(
                "BUG: [IgnoreOnOneBranch] should have prevented this test from running on an OneBranch build. " +
                "If this failure appears when ONEBRANCH_BUILD is defined, the skip logic is broken.");
        }

        /// <summary>
        /// Wrong ordering: [DoNotRunOnLinux] first shadows [IgnoreOnOneBranch].
        /// On an OneBranch build on Windows, [DoNotRunOnLinux] sees a non-Linux platform,
        /// passes through to base.ExecuteAsync, and [IgnoreOnOneBranch] is never consulted.
        /// The test body runs and hits Assert.Fail, demonstrating the bug.
        /// </summary>
        [DoNotRunOnLinux]
        [IgnoreOnOneBranch]
        public void WrongOrder_DoNotRunOnLinuxFirst_RunsOnOneBranchWhenItShouldnt()
        {
            Assert.Fail(
                "BUG DEMONSTRATED: This test reached its body on an OneBranch build because [DoNotRunOnLinux] " +
                "was listed first and shadowed [IgnoreOnOneBranch]. " +
                "Fix: move [IgnoreOnOneBranch] to be the first attribute.");
        }

        // -----------------------------------------------------------------------------------------
        // FIX DEMONSTRATION: new-style [RunOn] with SkipConditions flags
        // All conditions are evaluated inside a single ExecuteAsync — ordering is impossible to
        // get wrong because there is only one attribute.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// New style: single [RunOn] attribute with combined SkipConditions flags.
        /// On an OneBranch build (Windows agent) the OneBranchBuild condition fires and the
        /// test is skipped before its body is reached, regardless of how many conditions are listed.
        /// </summary>
        [RunOn(SkipConditions.OneBranchBuild | SkipConditions.Linux)]
        public void NewStyle_CombinedConditions_SkippedOnOneBranchBuild()
        {
            Assert.Fail(
                "BUG: SkipConditions.OneBranchBuild should have skipped this test. " +
                "If this failure appears when ONEBRANCH_BUILD is defined, EvaluateSkipConditions is broken.");
        }

        /// <summary>
        /// New style: demonstrates that the order of flags inside SkipConditions is irrelevant.
        /// This is logically identical to NewStyle_CombinedConditions_SkippedOnOneBranchBuild
        /// (Linux and OneBranchBuild are swapped) but produces the same skip behavior —
        /// proving the old fragile ordering convention is no longer needed.
        /// </summary>
        [RunOn(SkipConditions.Linux | SkipConditions.OneBranchBuild)]
        public void NewStyle_FlagOrderIsIrrelevant_AlsoSkippedOnOneBranchBuild()
        {
            Assert.Fail(
                "BUG: SkipConditions.OneBranchBuild should have skipped this test. " +
                "Flag order in SkipConditions is irrelevant — all conditions are evaluated together.");
        }
    }
}
#endif
