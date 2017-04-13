using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Identity.Client;
using Xamarin.Forms;
using XForms.Droid;

[assembly: Dependency(typeof(AndroidAcquireToken))]

namespace XForms.Droid
{
    internal class AndroidAcquireToken : IAcquireToken
    {
        public Task<AuthenticationResult> AcquireTokenAsync(PublicClientApplication app, string[] scopes, UIParent uiParent)
        {
            return app.AcquireTokenAsync(scopes, uiParent);
        }

        public Task<AuthenticationResult> AcquireTokenAsync(PublicClientApplication app, string[] scopes, string loginHint, UIParent uiParent)
        {
            return app.AcquireTokenAsync(scopes, loginHint, uiParent);
        }
    }
}