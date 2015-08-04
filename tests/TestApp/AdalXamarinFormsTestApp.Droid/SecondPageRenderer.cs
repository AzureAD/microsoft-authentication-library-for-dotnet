using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AdalXamarinFormsTestApp.Droid;
using AdalXamarinFormsTestApp;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

[assembly: ExportRenderer(typeof(SecondPage), typeof(SecondPageRenderer))]
namespace AdalXamarinFormsTestApp.Droid
{
    class SecondPageRenderer : PageRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            SecondPage page = e.NewElement as SecondPage;

            var activity = this.Context as Activity;
            
            page.Paramters = new PlatformParameters(activity);
        }
    }
}