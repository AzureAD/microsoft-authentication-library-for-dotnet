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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Indicates whether AcquireToken should automatically prompt only if necessary or whether
    /// it should prompt regardless of whether there is a cached token.
    /// </summary>
    public enum PromptBehavior
    {
        /// <summary>
        /// Acquire token will prompt the user for credentials only when necessary.  If a token
        /// that meets the requirements is already cached then the user will not be prompted.
        /// </summary>
        Auto,

        /// <summary>
        /// The user will be prompted for credentials even if there is a token that meets the requirements
        /// already in the cache.
        /// </summary>
        Always,

        /// <summary>
        /// The user will not be prompted for credentials.  If prompting is necessary then the AcquireToken request
        /// will fail.
        /// </summary>
        Never,

        /// <summary>
        /// Re-authorizes (through displaying webview) the resource usage, making sure that the resulting access
        /// token contains updated claims. If user logon cookies are available, the user will not be asked for 
        /// credentials again and the logon dialog will dismiss automatically.
        /// </summary>
        RefreshSession
    }
}
