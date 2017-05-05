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

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    [Android.Runtime.Preserve(AllMembers = true)]
    internal class PlatformInformation : PlatformInformationBase
    {
        internal const string AndroidDefaultRedirectUriTemplate = "msal{0}://auth";

        public PlatformInformation(RequestContext requestContext) : base(requestContext)
        {
        }

        public override string GetProductName()
        {
            return "MSAL.Xamarin.Android";
        }

        public override string GetEnvironmentVariable(string variable)
        {
            return null;
        }

        public override string GetProcessorArchitecture()
        {
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Lollipop)
            {
                return Android.OS.Build.CpuAbi;
            }

            IList<string> supportedABIs = Android.OS.Build.SupportedAbis;
            if (supportedABIs != null && supportedABIs.Count > 0)
            {
                return supportedABIs[0];
            }

            return null;
        }

        public override string GetOperatingSystem()
        {
            return Android.OS.Build.VERSION.Sdk;
        }

        public override string GetDeviceModel()
        {
            return Android.OS.Build.Model;
        }

        public override void ValidateRedirectUri(Uri redirectUri, RequestContext requestContext)
        {
            base.ValidateRedirectUri(redirectUri, requestContext);

            if (PlatformInformationBase.DefaultRedirectUri.Equals(redirectUri.AbsoluteUri))
                throw new MsalException(MsalError.RedirectUriValidationFailed, "Default redirect URI - " + PlatformInformationBase.DefaultRedirectUri +
                                        " can not be used on Android platform");
        }

        public override string GetDefaultRedirectUri(string clientId)
        {
            return string.Format(AndroidDefaultRedirectUriTemplate, clientId);
        }
    }
}