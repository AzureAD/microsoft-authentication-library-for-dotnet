// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

#if DESKTOP
            return resourceName;
#else
            return "Resources\\" + resourceName;
#endif
        }
    }

#if WINDOWS_APP
    /// <summary>
    /// On .net, this attribute is needed to copy resources to the test, which are
    /// placed in a directory similar to TestRun/date/out
    /// On other platforms, mstest runs the tests directly from bin, so this isn't needed.
    /// On netcore, this attribute has been implemented with NOP by mstest.
    /// On uwp, this attribute is missing completely.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple =true)]
    public class DeploymentItemAttribute : System.Attribute
    {
        public DeploymentItemAttribute(string path)
        {
            // do nothing, on platforms other than .net
            // deployment happens by way of copying resources to the bin folder
        }

        public DeploymentItemAttribute(string path, string deploymentPath)
        {
            // do nothing, on platforms other than .net
            // deployment happens by way of copying resources to the bin folder
        }
    }
#endif
}
