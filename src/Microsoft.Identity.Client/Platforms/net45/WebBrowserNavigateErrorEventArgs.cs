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

using System.ComponentModel;

namespace Microsoft.Identity.Client.Platforms.net45
{
    /// <summary>
    /// </summary>
    public class WebBrowserNavigateErrorEventArgs : CancelEventArgs
    {
        /// <summary>
        /// </summary>
        public WebBrowserNavigateErrorEventArgs(string url, string targetFrameName, int statusCode,
            object webBrowserActiveXInstance)
        {
            Url = url;
            TargetFrameName = targetFrameName;
            StatusCode = statusCode;
            WebBrowserActiveXInstance = webBrowserActiveXInstance;
        }

        /// <summary>
        /// </summary>
        public string TargetFrameName { get; }

        // url as a string, as in case of error it could be invalid url
        /// <summary>
        /// </summary>
        public string Url {get;}

        /// <summary>
        /// </summary>
        public object WebBrowserActiveXInstance {get;}

        /// <summary>
        /// </summary>
        public int StatusCode { get; }
    }
}
