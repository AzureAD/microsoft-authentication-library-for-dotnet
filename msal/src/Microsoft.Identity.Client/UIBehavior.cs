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

    //TODO: This should be completely removed for platforms that do not support UI, however 
    // at present it used for ConfidentialClientApplication.GetAuthorizationRequestUrlAsync
#if !NET_CORE
    public
#else
    internal 
#endif    
        struct UIBehavior
    {
        /// <summary>
        /// AcquireToken will send prompt=select_account to authorize endpoint 
        /// and would show a list of users from which one can be selected for 
        /// authentication.
        /// </summary>
        public static readonly UIBehavior SelectAccount = new UIBehavior("select_account");

        /// <summary>
        /// The user will be prompted for credentials by the service. It is achieved
        /// by sending prompt=login to the service.
        /// </summary>
        public static readonly UIBehavior ForceLogin = new UIBehavior("login");

        /// <summary>
        /// The user will be prompted to consent even if consent was granted before. It is achieved
        /// by sending prompt=consent to the service.
        /// </summary>
        public static readonly UIBehavior Consent = new UIBehavior("consent");


#if DESKTOP || WINDOWS_APP
        /// <summary>
        /// Only available on .NET platform. AcquireToken will send prompt=attempt_none to 
        /// authorize endpoint and the library uses a hidden webview to authenticate the user.
        /// </summary>
        public static readonly UIBehavior Never = new UIBehavior("attempt_none");
#endif

        internal string PromptValue { get; }

        private UIBehavior(string promptValue)
        {
            PromptValue = promptValue;
        }

        /// <summary>
        /// Equals method override to compare UIBehavior structs
        /// </summary>
        /// <param name="obj">object to compare against</param>
        /// <returns>true if object are equal.</returns>
        public override bool Equals(object obj)
        {
            return obj is UIBehavior && this == (UIBehavior)obj;
        }

        /// <summary>
        /// Override to compute hashcode
        /// </summary>
        /// <returns>hash code of the PromptValue</returns>
        public override int GetHashCode()
        {
            return PromptValue.GetHashCode();
        }

        /// <summary>
        /// operator overload to equality check
        /// </summary>
        /// <param name="x">first value</param>
        /// <param name="y">second value</param>
        /// <returns>true if the object are equal</returns>
        public static bool operator ==(UIBehavior x, UIBehavior y)
        {
            return x.PromptValue == y.PromptValue;
        }

        /// <summary>
        /// operator overload to equality check
        /// </summary>
        /// <param name="x">first value</param>
        /// <param name="y">second value</param>
        /// <returns>true if the object are not equal</returns>
        public static bool operator !=(UIBehavior x, UIBehavior y)
        {
            return !(x == y);
        }
    }
}
