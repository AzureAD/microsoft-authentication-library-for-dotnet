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

using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.TelemetryCore;
using System;
using Microsoft.Identity.Client.Config;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client
{
    //TODO: minor bug - we accidentally exposed this ctor to UWP without exposing
    // the TokenCacheExtensions. Not worth removing and breaking backwards compat for it now, 
    // as we plan to expose the whole thing
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME
    public sealed partial class PublicClientApplication : ClientApplicationBase
    {
        /// <summary>
        /// Constructor to create application instance. This constructor is only available for Desktop and NetCore apps
        /// </summary>
        /// <param name="clientId">Client id of the application</param>
        /// <param name="authority">Default authority to be used for the application</param>
        /// <param name="userTokenCache">Instance of TokenCache.</param>
        public PublicClientApplication(string clientId, string authority, TokenCache userTokenCache)
            : this(PublicClientApplicationBuilder.Create(clientId, authority).WithUserTokenCache(userTokenCache).BuildConfiguration())
        {
            GuardOnMobilePlatforms();
        }

        private static void GuardOnMobilePlatforms()
        {
#if ANDROID || iOS
        throw new PlatformNotSupportedException("You should not use this constructor that takes in a TokenCache object on mobile platforms. " +
            "This constructor is meant to allow applications to define their own storage strategy on .net desktop and .net core. " +
            "On mobile platforms, a secure and performant storage mechanism is implemeted by MSAL. " +
            "For more details about custom token cache serialization, visit https://aka.ms/msal-net-serialization");
#endif
        }
    }

#endif
}
