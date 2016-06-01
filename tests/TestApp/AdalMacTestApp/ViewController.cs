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

using System;

using AppKit;
using Foundation;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using TestApp.PCL;

namespace AdalMacTestApp
{
    public partial class ViewController : NSViewController
    {
        //
        //NOTE: Replace these with valid values
        //
        const string AUTHORITY = "https://login.windows.net/common";
        const string CLIENTID = "<CLIENTID>";
        const string RESOURCE = "<RESOURCE>";
        const string USER = null;
        const string REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";

        MobileAppSts sts = new MobileAppSts();

        TokenBroker CreateBrokerWithSts ()
        {
            var tokenBroker = new TokenBroker();

            sts.Authority = AUTHORITY;
            sts.ValidClientId = CLIENTID;
            sts.ValidResource = RESOURCE;
            sts.ValidUserName = USER;
            sts.ValidNonExistingRedirectUri = new Uri(REDIRECT_URI);
            tokenBroker.Sts = sts;
            return tokenBroker;
        }

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        async partial void AcquireInteractiveClicked(NSObject sender)
        {
            textView.Value = string.Empty;
            string token = await CreateBrokerWithSts().GetTokenInteractiveAsync(new PlatformParameters(this.View.Window, false) { UseModalDialog = true });
            textView.Value = token;
        }

        async partial void AcquireSilentClicked(NSObject sender)
        {
            textView.Value = string.Empty;
            string token = await CreateBrokerWithSts ().GetTokenSilentAsync(new PlatformParameters(View.Window, false) { UseModalDialog = true });
            textView.Value = token;
        }

        partial void ClearCacheClicked(NSObject sender)
        {
            var tokenBroker = new TokenBroker();
            tokenBroker.ClearTokenCache();
        }
    }
}
