//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

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

            AdalInitializer.Initialize();
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
                sts.ValidClientId = "b92e0ba5-f86e-4411-8e18-6b5f928d968a";
                sts.ValidResource = "https://msdevex-my.sharepoint.com";
                sts.ValidUserName = "user@msdevex.onmicrosoft.com";
                sts.ValidNonExistingRedirectUri = new Uri("adaliosapp://com.your-company.adaliostestapp");
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
                sts.ValidClientId = "b92e0ba5-f86e-4411-8e18-6b5f928d968a";
                sts.ValidResource = "https://msdevex-my.sharepoint.com";
                sts.ValidUserName = "user@msdevex.onmicrosoft.com";
                sts.ValidNonExistingRedirectUri = new Uri("adaliosapp://com.your-company.adaliostestapp");
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