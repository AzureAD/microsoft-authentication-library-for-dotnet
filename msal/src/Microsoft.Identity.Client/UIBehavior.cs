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
    /// Stucture containing static members that you can use to specify how the interactive overrides 
    /// of AcquireTokenAsync in <see cref="PublicClientApplication"/> should prompt the user.
    /// </summary>
    /// <remarks>Only the .NET Framework platforms allows <c>UIBehavior.Never</c></remarks>

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
        /// AcquireToken will send <c>prompt=select_account</c> to Azure AD's authorize endpoint 
        /// which would present to the user a list of accounts from which one can be selected for 
        /// authentication.
        /// </summary>
        public static readonly UIBehavior SelectAccount = new UIBehavior("select_account");

        /// <summary>
        /// The user will be prompted for credentials by the service. It is achieved
        /// by sending <c>prompt=login</c> to the Azure AD service.
        /// </summary>
        public static readonly UIBehavior ForceLogin = new UIBehavior("login");

        /// <summary>
        /// The user will be prompted to consent even if consent was granted before. It is achieved
        /// by sending <c>prompt=consent</c> to Azure AD.
        /// </summary>
        public static readonly UIBehavior Consent = new UIBehavior("consent");

        /// <summary>
        /// Does not request any specific UI to the service, which therefore decides based on the 
        /// number of signed-in identities. 
        /// This UIBehavior is, for the moment, recommended for Azure AD B2C scenarios where
        /// the developer does not want the user to re-select the account (for instance apply 
        /// policies like EditProfile, or ResetPassword, which should apply to the currently signed-in account.
        /// It's not recommended to use this UIBehavior in Azure AD scenarios for the moment.
        /// </summary>
        public static readonly UIBehavior NoPrompt = new UIBehavior("no_prompt");

#if DESKTOP || WINDOWS_APP
        /// <summary>
        /// Only available on .NET platform. AcquireToken will send <c>prompt=attempt_none</c> to 
        /// Azure AD's authorize endpoint and the library will use a hidden webview (and its cookies) to authenticate the user.
        /// This can fail, and in that case a <see cref="MsalUiRequiredException"/> will be thrown.
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
        /// <returns>true if the objects are equal</returns>
        public static bool operator ==(UIBehavior x, UIBehavior y)
        {
            return x.PromptValue == y.PromptValue;
        }

        /// <summary>
        /// operator overload to equality check
        /// </summary>
        /// <param name="x">first value</param>
        /// <param name="y">second value</param>
        /// <returns>true if the objects are not equal</returns>
        public static bool operator !=(UIBehavior x, UIBehavior y)
        {
            return !(x == y);
        }
    }
}
