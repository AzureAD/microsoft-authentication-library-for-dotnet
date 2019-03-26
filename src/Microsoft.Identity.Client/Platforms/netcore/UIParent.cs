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
using Microsoft.Identity.Client.Exceptions;

namespace Microsoft.Identity.Client
{
    // This entire class exists only at runtime, to provide avoid MissingMethodException on NetStandard
#if NET_CORE_RUNTIME

    /// <summary>
    /// Allows for configuration of the web UI experience. Not supported on .net core
    /// </summary>     
    public sealed class UIParent
    {
        /// <summary>
        /// Default constructor. Will throw a PlatformNotSupported exception on .netcore because .netcore does not support Interactive Flows. 
        /// </summary>
        /// <remarks>Consider using Device Code Flow https://aka.ms/msal-device-code-flow or Integrated Windows Auth https://aka.ms/msal-net-iwa </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public UIParent()
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }

        /// <summary>
        /// Platform agnostic constructor that allows building an UIParent from a NetStandard assembly.
        /// Will throw a PlatformNotSupported exception on .netcore because .netcore does not support Interactive Flows. 
        /// </summary>
        /// <remarks>Consider using Device Code Flow https://aka.ms/msal-device-code-flow or Integrated Windows Auth https://aka.ms/msal-net-iwa </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public UIParent(object parent, bool useEmbeddedWebview)
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }

        /// <summary>
        /// Checks if the system weview can be used. 
        /// Currently, on .NET Core, no webviews are available, so this throws.
        /// </summary>
        public static bool IsSystemWebviewAvailable() // This is part of the NetStandard "interface" 
        {
            return false;
        }
    }
#endif
}
