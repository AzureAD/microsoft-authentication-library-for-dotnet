using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using AdalXamarinFormsTestApp;
using AdalXamarinFormsTestApp.iOS;
using System.Drawing;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

[assembly: ExportRenderer(typeof(SecondPage), typeof(SecondPageRenderer))]
namespace AdalXamarinFormsTestApp.iOS
{
    class SecondPageRenderer : PageRenderer
    {
        SecondPage page;

        protected override void OnElementChanged (VisualElementChangedEventArgs e)
        {
            base.OnElementChanged (e);

            page = e.NewElement as SecondPage;
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            page.Paramters = new PlatformParameters(this);
        }
    }
}