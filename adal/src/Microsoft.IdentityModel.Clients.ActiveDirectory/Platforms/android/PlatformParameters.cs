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

using Android.App;
using System;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Additional parameters used in acquiring user's authorization
    /// </summary>
    [CLSCompliant(false)]
    public class PlatformParameters : IPlatformParameters
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="callerActivity">activity calling ADAL to acquire token</param>
        public PlatformParameters(Activity callerActivity):this(callerActivity, false, PromptBehavior.Auto)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="callerActivity">activity calling ADAL to acquire token</param>
        /// <param name="useBroker">flag to enable or disable broker flow. FALSE by default.</param>
        public PlatformParameters(Activity callerActivity, bool useBroker):this(callerActivity, useBroker, PromptBehavior.Auto)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="callerActivity">activity calling ADAL to acquire token</param>
        /// <param name="useBroker">flag to enable or disable broker flow. FALSE by default.</param>
        /// <param name="promptBehavior">Prompt behavior enum to control UI</param>
        public PlatformParameters(Activity callerActivity, bool useBroker, PromptBehavior promptBehavior)
        {
            this.CallerActivity = callerActivity;
            UseBroker = useBroker;
            PromptBehavior = promptBehavior;
        }

        /// <summary>
        /// Flag to enable or disable broker flow. FALSE by default.
        /// </summary>
        public bool UseBroker { get; set; }

        /// <summary>
        /// Caller Android Activity
        /// </summary>
        public Activity CallerActivity { get; private set; }

        /// <summary>
        /// Gets prompt behavior. If <see cref="PromptBehavior.Always"/>, asks service to show user the authentication page which gives them chance to authenticate as a different user.
        /// </summary>
        public PromptBehavior PromptBehavior { get; set; }
    }
}
