//------------------------------------------------------------------------------
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

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Indicates how AcquireToken should prompt the user.
    /// </summary>
    public partial struct UIOptions
    {
        /// <summary>
        /// AcquireToken will send prompt=select_account to authorize endpoint 
        /// and would show a list of users from which one can be selected for 
        /// authentication.
        /// </summary>
        public readonly static UIOptions SelectAccount = new UIOptions("select_account");

        /// <summary>
        /// The user will be prompted for credentials by the service. It is achieved
        /// by sending prompt=login to the service.
        /// </summary>
        public readonly static UIOptions ForceLogin = new UIOptions("login");

        /// <summary>
        /// The user will be prompted to consent even if consent was granted before. It is achieved
        /// by sending prompt=consent to the service.
        /// </summary>
        public readonly static UIOptions Consent = new UIOptions("consent");

        internal string PromptValue { get; set; }

        internal UIOptions(string promptValue)
        {
            PromptValue = promptValue;
        }
    }
}