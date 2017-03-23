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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Identity.Client;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AcquirePage : ContentPage
    {
        public IPlatformParameters platformParameters { get; set; }

        public AcquirePage()
        {
            InitializeComponent();
        }

        private void SetPlatformParameters()
        {
            App.PCA.PlatformParameters = platformParameters;
        }

        protected override void OnAppearing()
        {
            SetPlatformParameters();
        }

        private async void OnAcquireClicked(object sender, EventArgs e)
        {

            if (App.PCA.PlatformParameters == null)
            {
                SetPlatformParameters();
            }

            try
            {
                AuthenticationResult res = await App.PCA.AcquireTokenAsync(App.Scopes);

                acquireResponseLabel.Text = "Result - " + res.AccessToken;

            }
            catch (MsalException exception)
            {
                acquireResponseLabel.Text = "MsalException - " + exception;
            }
            catch (Exception exception)
            {
                acquireResponseLabel.Text = "Exception - " + exception;
            }

        }
    }
}

