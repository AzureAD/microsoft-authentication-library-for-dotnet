// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    public class ResourceHelper
    {
        /// <summary>
        /// Gets the relative path to a test resource. Resource should be using DeploymentItem (desktop) or
        /// by setting Copy to Output Directory to Always (other platforms)
        /// </summary>
        /// <remarks>
        /// This is just a simple workaround for DeploymentItem not being implemented in mstest on netcore
        /// Tests seems to run from the bin directory and not from a TestRun dir on netcore
        /// Assumes resources are in a Resources dir.
        /// Note that conditional compilation files cannot live in the common projects unless
        /// the flags are replicated.
        /// </remarks>
        public static string GetTestResourceRelativePath(string resourceName)
        {
            if (!File.Exists(resourceName))
            {
                Assert.Fail($"Test resource {resourceName} not found. Please ensure that the resource is marked with DeploymentItem attribute.");
            }
            return resourceName;
        }
    }
}
