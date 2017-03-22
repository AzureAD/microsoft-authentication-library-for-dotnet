using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Text;
using XForms;
using XForms.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
[assembly: ExportRenderer(typeof(AcquirePage), typeof(AcquirePageRenderer))]

namespace XForms.iOS
{
    class AcquirePageRenderer : PageRenderer
    {
        AcquirePage page;
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            page = e.NewElement as AcquirePage;
        }
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            page.platformParameters = new PlatformParameters(this);
        }
    }
}
