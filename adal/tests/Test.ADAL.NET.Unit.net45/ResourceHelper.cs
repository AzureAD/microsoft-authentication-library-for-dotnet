//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

namespace Test.Microsoft.Identity.Core.Unit
{    
    public class ResourceHelper
    {
        /// <summary>
        /// Gets the relative path to a test resource that is deployed to the test, across
        /// netcore and net desktop.
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
}
