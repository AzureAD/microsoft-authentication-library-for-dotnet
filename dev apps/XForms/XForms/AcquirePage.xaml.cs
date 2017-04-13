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
        public UIParent UIParent { get; set; }

        public AcquirePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            authority.Text = App.Authority;
            validateAuthority.IsToggled = App.ValidateAuthority;
        }

        private string ToString(IUser user)
        {
            var sb = new StringBuilder();

            sb.AppendLine("user.DisplayableId : " + user.DisplayableId);
            sb.AppendLine("user.IdentityProvider : " + user.IdentityProvider);
            sb.AppendLine("user.Name : " + user.Name);

            return sb.ToString();
        }

        private string ToString(AuthenticationResult result)
        {
            var sb = new StringBuilder();

            sb.AppendLine("AccessToken : " + result.AccessToken);
            sb.AppendLine("IdToken : " + result.IdToken);
            sb.AppendLine("ExpiresOn : " + result.ExpiresOn);
            sb.AppendLine("TenantId : " + result.TenantId);
            sb.AppendLine("Scope : " + string.Join(",", result.Scope));
            sb.AppendLine("User :");
            sb.Append(ToString(result.User));

            return sb.ToString();
        }

        private IUser getUserByDisplayableId(string str)
        {
            return App.MsalPublicClient.Users.FirstOrDefault(user => user.DisplayableId.Equals(str));
        }

        private async void OnAcquireSilentlyClicked(object sender, EventArgs e)
        {
            acquireResponseLabel.Text = "Starting silent token acquisition";
            await Task.Delay(700);

            try
            {
                var user = getUserByDisplayableId(UserEntry.Text.Trim());
                if (user == null)
                {
                    acquireResponseLabel.Text = "User - \"" + UserEntry.Text.Trim() + "\" was not found in the cache";
                    return;
                }
                var res = await App.MsalPublicClient.AcquireTokenSilentAsync(App.Scopes, user);

                acquireResponseLabel.Text = ToString(res);
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

        private async void OnAcquireClicked(object sender, EventArgs e)
        {
            IAcquireToken at = Device.OS == TargetPlatform.Android ? DependencyService.Get<IAcquireToken>() : new AcquireToken();

            try
            {
                AuthenticationResult res;
                if (LoginHint.IsToggled)
                {
                    res = await at.AcquireTokenAsync(App.MsalPublicClient, App.Scopes, UserEntry.Text.Trim(), UIParent);
                }
                else
                {
                    res = await at.AcquireTokenAsync(App.MsalPublicClient, App.Scopes, UIParent);
                }
                acquireResponseLabel.Text = ToString(res);
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

        private void OnClearClicked(object sender, EventArgs e)
        {
            acquireResponseLabel.Text = "";
        }

        private void OnValidateAuthorityToggled(object sender, ToggledEventArgs args)
        {
            App.MsalPublicClient.ValidateAuthority = args.Value;
            App.ValidateAuthority = args.Value;
        }
    }
}

