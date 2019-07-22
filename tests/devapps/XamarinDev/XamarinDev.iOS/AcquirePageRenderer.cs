// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using XamarinDev;
using XamarinDev.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(AcquirePage), typeof(AcquirePageRenderer))]

namespace XamarinDev.iOS
{
    internal class AcquirePageRenderer : PageRenderer
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
        }
    }
}
