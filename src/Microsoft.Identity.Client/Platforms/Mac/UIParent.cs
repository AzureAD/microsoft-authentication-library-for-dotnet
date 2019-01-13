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
using AppKit;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Allows for configuration of the web UI experience. 
    /// </summary>     
    public sealed class UIParent
    {
        internal CoreUIParent CoreUIParent { get; }

        /// <summary>
        /// Default constructor. Uses the NSApplication.SharedApplication.MainWindow to parent the web ui.
        /// </summary>
        public UIParent()
        {
            CoreUIParent = new CoreUIParent(NSApplication.SharedApplication.MainWindow);
        }

#pragma warning disable CS3001 // Argument type is not CLS-compliant
                              /// <summary>
                              /// Create a UIParent given an instance of NSWindow, which will be used to parent the web ui.
                              /// </summary>
        public UIParent(NSWindow callerWindow)
#pragma warning restore CS3001 // Argument type is not CLS-compliant
        {
            if (callerWindow == null)
            {
                callerWindow = NSApplication.SharedApplication.MainWindow;
            }

            CoreUIParent = new CoreUIParent(callerWindow);
        }


#if MAC_RUNTIME
        /// <summary>
        /// Platform agnostic constructor that allows building an UIParent from a NetStandard assembly.
        /// </summary>
        /// <remarks>This constructor is only avaiable at runtime, to provide support for NetStandard</remarks>
        /// <param name="parent">Expected to be a NSWindow instance. Passing null implies the MainWindow will be used</param>
        /// <param name="useEmbeddedWebview">Ignored, the embedded view is always used on Mac</param>
        public UIParent(object parent, bool useEmbeddedWebview) :
            this(ValidateParentObject(parent))
        {
            
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

        private static NSWindow ValidateParentObject(object parent)
        {
            if (parent == null)
            {
                return null;
            }           

            NSWindow parentActivity = parent as NSWindow;
            if (parentActivity == null)
            {
                throw new ArgumentException(nameof(parent) +
                                            " is expected to be of type NSWindow but is of type " +
                                            parent.GetType());
            }

            return parentActivity;
        }

    }
}
