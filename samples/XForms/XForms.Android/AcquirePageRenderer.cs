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
using Microsoft.Identity.Client;
using Xamarin.Forms.Platform.Android;
using XForms;
using Xamarin.Forms;
using XForms.Droid;
[assembly: ExportRenderer(typeof(AcquirePage), typeof(AcquirePageRenderer))]

namespace XForms.Droid
{
    class AcquirePageRenderer : PageRenderer
    {
        AcquirePage page;

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);
            page = e.NewElement as AcquirePage;
            var activity = this.Context as Activity;
            page.platformParameters = new PlatformParameters(activity);
        }
    }
}