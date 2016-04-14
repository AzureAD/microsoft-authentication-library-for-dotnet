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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Additional parameters used in acquiring user's authorization
    /// </summary>
    public class PlatformParameters : IPlatformParameters
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="promptBehavior">Prompt behavior enum to control UI</param>
        /// <param name="useCorporateNetwork">flag to enable logged in user authentication</param>
        public PlatformParameters(PromptBehavior promptBehavior, bool useCorporateNetwork)
        {
            this.PromptBehavior = promptBehavior;
            this.UseCorporateNetwork = useCorporateNetwork;
        }

        /// <summary>
        /// Gets or Sets flag to enable logged in user authentication. Note that enabling this flag requires some extra application capabilites.
        /// This flag only works in SSO mode and is ignored otherwise. To enable SSO mode, call AcquireTokenAsync with null or application's callback URI as redirectUri.
        /// </summary>
        public bool UseCorporateNetwork { get; private set; }

        /// <summary>
        /// Gets prompt behavior. If <see cref="PromptBehavior.Always"/>, asks service to show user the authentication page which gives them chance to authenticate as a different user.
        /// </summary>
        public PromptBehavior PromptBehavior { get; private set; }
    }
}
