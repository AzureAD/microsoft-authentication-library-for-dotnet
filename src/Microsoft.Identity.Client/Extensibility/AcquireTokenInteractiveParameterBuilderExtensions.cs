// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using Microsoft.Identity.Client.ApiConfig;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// </summary>
    public static class AcquireTokenInteractiveParameterBuilderExtensions
    {
        /// <summary>
        ///     Extension method enabling MSAL.NET extenders for public client applications to set a custom web ui
        ///     that will let the user sign-in with Azure AD, present consent if needed, and get back the authorization
        ///     code
        /// </summary>
        /// <param name="builder">Builder for an AcquireTokenInteractive</param>
        /// <param name="customWebUi">Customer implementation for the Web UI</param>
        /// <returns>the builder to be able to chain .With methods</returns>
        public static AcquireTokenInteractiveParameterBuilder WithCustomWebUi(
            this AcquireTokenInteractiveParameterBuilder builder,
            ICustomWebUi customWebUi)
        {
            builder.SetCustomWebUi(customWebUi);
            return builder;
        }
    }
}