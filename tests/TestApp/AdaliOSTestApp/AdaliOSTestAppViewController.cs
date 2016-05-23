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
using Foundation;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

using UIKit;

using TestApp.PCL;

namespace AdaliOSTestApp
{
    public partial class AdaliOSTestAppViewController : UIViewController
    {
        MobileAppSts sts = new  MobileAppSts();
        public AdaliOSTestAppViewController(IntPtr handle)
            : base(handle)
        {
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();

            // Release any cached data, images, etc that aren't in use.
        }

        #region View lifecycle

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
        }

        #endregion

        async partial void UIButton11_TouchUpInside(UIButton sender)
        {
            try
            {
                ReportLabel.Text = string.Empty;
                TokenBroker tokenBroker = new TokenBroker();

                sts.Authority = "https://login.microsoftonline.com/common";
                sts.ValidClientId = "<CLIENT_ID>";
                sts.ValidResource = "<RESOURCE>";
                sts.ValidUserName = "<USER>";
                sts.ValidNonExistingRedirectUri = new Uri("REDIRECT_URI");
                tokenBroker.Sts = sts;
                string token = await tokenBroker.GetTokenInteractiveAsync(new PlatformParameters(this, false));
                ReportLabel.Text = token;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        async partial void UIButton16_TouchUpInside(UIButton sender)
        {
            try
            {
                ReportLabel.Text = string.Empty;
                TokenBroker tokenBroker = new TokenBroker();
                sts.Authority = "https://login.microsoftonline.com/common";
                sts.ValidClientId = "<CLIENT_ID>";
                sts.ValidResource = "<RESOURCE>";
                sts.ValidUserName = "<USER>";
                sts.ValidNonExistingRedirectUri = new Uri("REDIRECT_URI");
                tokenBroker.Sts = sts;
                string token = await tokenBroker.GetTokenInteractiveAsync(new PlatformParameters(this, false));
                ReportLabel.Text = token;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        partial void UIButton30_TouchUpInside(UIButton sender)
        {
            TokenBroker tokenBroker = new TokenBroker();
            tokenBroker.ClearTokenCache();
        }
    }
}
