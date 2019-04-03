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

using System;
using System.ComponentModel;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Allows for configuration of the web UI experience. 
    /// </summary>     
    public sealed class UIParent
    {
        /// <summary>
        /// Default constructor. Uses the NSApplication.SharedApplication.MainWindow to parent the web ui.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public UIParent()
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }


#if MAC_RUNTIME
        /// <summary>
        /// Platform agnostic constructor that allows building an UIParent from a NetStandard assembly.
        /// </summary>
        /// <remarks>This constructor is only avaiable at runtime, to provide support for NetStandard</remarks>
        /// <param name="parent">Expected to be a NSWindow instance. Passing null implies the MainWindow will be used</param>
        /// <param name="useEmbeddedWebview">Ignored, the embedded view is always used on Mac</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public UIParent(object parent, bool useEmbeddedWebview) :
            this(ValidateParentObject(parent))
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }
#endif

        /// <summary>
        /// Checks if the system weview can be used. 
        /// Currently, on MAC, only the embedded webview is available, so this always returns false.
        /// </summary>
        public static bool IsSystemWebviewAvailable() // This is part of the NetStandard "interface" 
        {
            return false;
        }
    }
}
