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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    /// <summary>
    /// 
    /// </summary>
    public class WebBrowserNavigateErrorEventArgs : CancelEventArgs
    {
        // Fields
        private readonly string targetFrameName;
        private readonly string url;
        private readonly int statusCode;
        private readonly object webBrowserActiveXInstance;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="targetFrameName"></param>
        /// <param name="statusCode"></param>
        /// <param name="webBrowserActiveXInstance"></param>
        public WebBrowserNavigateErrorEventArgs(string url, string targetFrameName, int statusCode, object webBrowserActiveXInstance)
        {
            this.url = url;
            this.targetFrameName = targetFrameName;
            this.statusCode = statusCode;
            this.webBrowserActiveXInstance = webBrowserActiveXInstance;
        }

        /// <summary>
        /// 
        /// </summary>
        public string TargetFrameName
        {
            get
            {
                return this.targetFrameName;
            }
        }

        /// <summary>
        /// url as a string, as in case of error it could be invalid url
        /// </summary>
        public string Url
        {
            get
            {
                return this.url;
            }
        }

        /// <summary>
        /// ADAL.Native has code for interpretation of this code to string we don't do it here, as we need to come consideration should we do it or not.
        /// </summary>
        public int StatusCode
        {
            get
            {
                return this.statusCode;
            }
        }

        /// <summary>
        /// return object
        /// </summary>
        public object WebBrowserActiveXInstance
        {
            get
            {
                return this.webBrowserActiveXInstance;
            }
        }
    }
}
