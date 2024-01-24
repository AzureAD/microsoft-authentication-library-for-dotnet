// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Structure containing static members that you can use to specify how the interactive overrides
    /// of AcquireTokenAsync in <see cref="IPublicClientApplication" /> should prompt the user.
    /// </summary>
    public struct Prompt
    {
        /// <summary>
        /// AcquireToken will send <c>prompt=select_account</c> to the authorization server's authorize endpoint.
        /// which would present to the user a list of accounts from which one can be selected for
        /// authentication.
        /// </summary>
        /// <remarks>
        /// This is the default prompt for backwards compatibility reasons. Please use <see cref="Prompt.NoPrompt"/> for the best user experience.
        /// </remarks>
        public static readonly Prompt SelectAccount = new Prompt("select_account");

        /// <summary>
        /// The user will be prompted for credentials by the service. It is achieved
        /// by sending <c>prompt=login</c> to the authorize endpoint.
        /// </summary>
        public static readonly Prompt ForceLogin = new Prompt("login");

        /// <summary>
        /// The user will be prompted to consent, even if consent was granted before. It is achieved
        /// by sending <c>prompt=consent</c> to the authorization server's authorize endpoint.
        /// </summary>
        public static readonly Prompt Consent = new Prompt("consent");

        /// <summary>
        /// Let the identity service decide on the best user experience, based on browser cookies and 
        /// on the login hint, which can be specified using WithAccount() or WithLoginHint()
        /// </summary>
        /// <remarks>This is the recommended prompt</remarks>
        public static readonly Prompt NoPrompt = new Prompt("no_prompt");

        /// <summary>
        /// AcquireToken will send <c>prompt=create</c> to the authorization server's authorize endpoint
        /// which would trigger a sign-up experience, used for External Identities. 
        /// </summary>
        /// <remarks>More details at https://aka.ms/msal-net-prompt-create. </remarks>
        public static readonly Prompt Create = new Prompt("create");

#if NETFRAMEWORK || WINDOWS_APP
        /// <summary>
        /// Only available on .NET platform. AcquireToken will send <c>prompt=attempt_none</c> to
        /// the authorization server's authorize endpoint and the library will use a hidden WebView (and its cookies) to authenticate the user.
        /// This can fail, and in that case a <see cref="MsalUiRequiredException"/> will be thrown.
        /// </summary>
        public static readonly Prompt Never = new Prompt("attempt_none");
#endif
        // for when the developer doesn't specify a prompt
        internal static readonly Prompt NotSpecified = new Prompt("not_specified");

        internal string PromptValue { get; }

        private Prompt(string promptValue)
        {
            PromptValue = promptValue;
        }

        /// <summary>
        /// Equals method override to compare Prompt structs
        /// </summary>
        /// <param name="obj">object to compare against</param>
        /// <returns>true if object are equal.</returns>
        public override bool Equals(object obj)
        {
            return obj is Prompt prompt && this == prompt;
        }

        /// <summary>
        /// Override to compute hash code
        /// </summary>
        /// <returns>hash code of the PromptValue</returns>
        public override int GetHashCode()
        {
            return PromptValue.GetHashCode();
        }

        /// <summary>
        /// Operator overload to check equality
        /// </summary>
        /// <param name="x">first value</param>
        /// <param name="y">second value</param>
        /// <returns>true if the objects are equal</returns>
        public static bool operator ==(Prompt x, Prompt y)
        {
            return x.PromptValue == y.PromptValue;
        }

        /// <summary>
        /// Operator overload to check inequality
        /// </summary>
        /// <param name="x">first value</param>
        /// <param name="y">second value</param>
        /// <returns>true if the objects are not equal</returns>
        public static bool operator !=(Prompt x, Prompt y)
        {
            return !(x == y);
        }
    }
}
