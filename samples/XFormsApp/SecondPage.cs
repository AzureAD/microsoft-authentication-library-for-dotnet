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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using TestApp.PCL;
using Xamarin.Forms;

namespace XFormsApp
{
    public class SecondPage : ContentPage
    {
        private TokenBroker tokenBroker;
        private Label result;

        public SecondPage()
        {
            this.tokenBroker = new TokenBroker();

            var browseButton = new Button
            {
                Text = "Acquire Token"
            };

            var clearButton = new Button
            {
                Text = "Clear Cache"
            };

            result = new Label { };

            browseButton.Clicked += browseButton_Clicked;
            clearButton.Clicked += clearButton_Clicked;

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children = {
                    browseButton,
                    clearButton,
                    result
				}
            };
        }

        public IPlatformParameters Paramters { get; set; }

        async void browseButton_Clicked(object sender, EventArgs e)
        {
            this.result.Text = string.Empty;
            string token = await tokenBroker.GetTokenInteractiveAsync(Paramters);
            this.result.Text = token;
        }

        void clearButton_Clicked(object sender, EventArgs e)
        {
            this.result.Text = this.result.Text = "Cache items before clear: " + TokenCache.DefaultShared.Count;
            tokenBroker.ClearTokenCache();
            this.result.Text = "Cache items after clear: " + TokenCache.DefaultShared.Count;
        }
    }
}
