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

using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Text;
using XForms;
using XForms.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Security;

[assembly: ExportRenderer(typeof(AcquirePage), typeof(AcquirePageRenderer))]

namespace XForms.iOS
{
    internal class AcquirePageRenderer : PageRenderer
    {
        AcquirePage page;
        private bool SubscribedToEvent = false;

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            page = e.NewElement as AcquirePage;

            if (!SubscribedToEvent)
            {
                App.MsalApplicationUpdated += OnMsalApplicationUpdated;
                SubscribedToEvent = true;
            }
            else
            {
                App.MsalApplicationUpdated -= OnMsalApplicationUpdated;
                App.MsalApplicationUpdated += OnMsalApplicationUpdated;
            }

            OnMsalApplicationUpdated(null, null);
        }

        private void OnMsalApplicationUpdated(object sender, EventArgs e)
        {
            App.MsalPublicClient.KeychainSecurityGroup = GetTeamId() + ".*";
        }

        private string GetTeamId()
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                Service = "",
                Account = "DotNetTeamIDHint",
                Accessible = SecAccessible.Always
            };

            SecRecord match = SecKeyChain.QueryAsRecord(queryRecord, out SecStatusCode resultCode);

            if (resultCode == SecStatusCode.ItemNotFound)
            {
                SecKeyChain.Add(queryRecord);
                match = SecKeyChain.QueryAsRecord(queryRecord, out resultCode);
            }

            if (resultCode == SecStatusCode.Success)
            {
                return match.AccessGroup.Split('.')[0];
            }

            return "";
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
        }
    }
}
